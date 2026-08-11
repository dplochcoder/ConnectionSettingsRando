using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MenuChanger.Attributes;

namespace ConnectionSettingsRando
{
    public class RandomizationStats
    {
        public List<string> RandomizedMembers = [];
        public List<string> EnforcedMembers = [];
        public List<string> SkippedMembers = [];
        public int RandomizedCount { get; set; }
        public int EnforcedCount { get; set; }
        public int SkippedCount { get; set; }
    }
    public class SettingsRandomizer
    {
        public RandomizationStats LastStats { get; private set; } = new();
        public T Randomize<T>(T settings, Random rng, string providerName)
            where T : new()
        {
            LastStats = new();
            T clone = Clone(settings);
            IReadOnlyList<string> path = [providerName];

            foreach (MemberInfo member in GetMembers(typeof(T)))
            {
                object value = GetValue(member, clone);
                object randomized = RandomizeValue(member, value, rng, path);
                SetValue(member, clone, randomized);
            }

            while (!ValidateDynamicBounds(clone))
            {
                foreach (MemberInfo member in GetMembers(typeof(T)))
                {
                    if (HasInvalidDynamicBounds(clone, member))
                    {
                        object value = GetValue(member, clone);
                        object randomized = RandomizeValue(member, value, rng, path);
                        SetValue(member, clone, randomized);
                    }
                }
            }
            return clone;
        }
        private object RandomizeValue(MemberInfo member, object value, Random rng, IReadOnlyList<string> path)
        {
            string name = string.Join(".", path.Append(member.Name));
            OptOutRule rule = RandoInterop.OptOutManager.GetRule(name);
            if (rule != null && rule.Action == OptOutAction.Exclude)
            {
                TrackSkip(member, path);
                return value;
            }
            Type type = GetMemberType(member);
            if (type == typeof(bool))
            {
                if (RandoInterop.Settings.IncludeBooleans)
                {
                    return RandomizeBool(member, path, rng);
                }
                else
                {
                    TrackSkip(member, path);
                    return value;
                }
            }

            if (IsNumeric(type))
            {
                if (RandoInterop.Settings.IncludeNumeric)
                {
                    TrackRando(member, path);
                    return RandomizeNumeric(member, rng);
                }
                else
                {
                    TrackSkip(member, path);
                    return value;
                }
            }

            if (type.IsEnum)
            {
                if (RandoInterop.Settings.IncludeCategorical)
                {
                    TrackRando(member, path);
                    return RandomizeEnum(type, rng);
                }
                else
                {
                    TrackSkip(member, path);
                    return value;
                }
            }
            
            if (!type.IsPrimitive && value != null)
            {
                IReadOnlyList<string> nestedPath = [.. path, member.Name];
                return RandomizeObject(value, rng, nestedPath);
            }

            TrackSkip(member, path);
            return value;
        }

        private static T Clone<T>(T source)
            where T : new()
        {
            T clone = new();

            foreach (MemberInfo member in GetMembers(typeof(T)))
            {
                SetValue(
                    member,
                    clone,
                    GetValue(member, source!));
            }

            return clone;
        }
        private object RandomizeBool(MemberInfo member, IReadOnlyList<string> path, Random rng)
        {
            string name = string.Join(".", path.Append(member.Name));
            OptOutRule rule = RandoInterop.OptOutManager.GetRule(name);
            if (rule?.Action == OptOutAction.ForceTrue)
            {
                TrackEnforce(member, path);
                return true;
            }
            if (rule?.Action == OptOutAction.ForceFalse)
            {
                TrackEnforce(member, path);
                return false;
            }
            
            TrackRando(member, path);
            return RandoInterop.Settings.SettingOdds > rng.NextDouble();
        }

        private object RandomizeNumeric(
            MemberInfo member,
            Random rng)
        {
            var range = member.GetCustomAttribute<MenuRangeAttribute>();

            switch (Type.GetTypeCode(GetMemberType(member)))
            {
                case TypeCode.Byte:
                {
                    byte min = Convert.ToByte(range?.min ?? byte.MinValue);
                    byte max = Convert.ToByte(range?.max ?? byte.MaxValue);

                    return (byte)rng.Next(min, max + 1);
                }

                case TypeCode.Int16:
                {
                    short min = Convert.ToInt16(range?.min ?? short.MinValue);
                    short max = Convert.ToInt16(range?.max ?? short.MaxValue);

                    return (short)rng.Next(min, max + 1);
                }

                case TypeCode.Int32:
                {
                    int min = Convert.ToInt32(range?.min ?? int.MinValue);
                    int max = Convert.ToInt32(range?.max ?? int.MaxValue);

                    return rng.Next(min, max + 1);
                }

                case TypeCode.Int64:
                {
                    long min = Convert.ToInt64(range?.min ?? long.MinValue);
                    long max = Convert.ToInt64(range?.max ?? long.MaxValue);

                    // Random doesn't support long directly
                    return min + (long)(rng.NextDouble() * (max - min + 1));
                }

                case TypeCode.Single:
                {
                    float min = Convert.ToSingle(range?.min ?? float.MinValue);
                    float max = Convert.ToSingle(range?.max ?? float.MaxValue);

                    return (float)(min + rng.NextDouble() * (max - min));
                }

                case TypeCode.Double:
                {
                    double min = Convert.ToDouble(range?.min ?? double.MinValue);
                    double max = Convert.ToDouble(range?.max ?? double.MaxValue);

                    return min + rng.NextDouble() * (max - min);
                }

                case TypeCode.Decimal:
                {
                    decimal min = Convert.ToDecimal(range?.min ?? decimal.MinValue);
                    decimal max = Convert.ToDecimal(range?.max ?? decimal.MaxValue);

                    return min + (decimal)rng.NextDouble() * (max - min);
                }

                default:
                    throw new NotSupportedException(
                        $"Unsupported numeric type {GetMemberType(member).Name}");
            }
        }
        private object RandomizeEnum(
            Type enumType,
            Random rng)
        {
            Array values = Enum.GetValues(enumType);
            return values.GetValue(rng.Next(values.Length))!;
        }
        private bool IsSettingsObject(Type type)
        {
            return type.IsClass
                && type != typeof(string)
                && type.GetConstructor(Type.EmptyTypes) != null;
        }
        private object RandomizeObject(object instance, Random rng, IReadOnlyList<string> path)
        {
            Type type = instance.GetType();
            if (!IsSettingsObject(type))
                return instance;

            object clone = Activator.CreateInstance(type)!;
            foreach (MemberInfo member in GetMembers(type))
            {
                object value = GetValue(member, instance);
                object randomized = RandomizeValue(member, value, rng, path);
                SetValue(member, clone, randomized);
            }

            while (!ValidateDynamicBounds(clone))
            {
                foreach (MemberInfo member in GetMembers(type))
                {
                    if (HasInvalidDynamicBounds(clone, member))
                    {
                        object value = GetValue(member, clone);
                        object randomized = RandomizeValue(member, value, rng, path);
                        SetValue(member, clone, randomized);
                    }
                }
            }

            return clone;
        }
        private static bool ValidateDynamicBounds(object settings)
        {
            foreach (MemberInfo member in GetMembers(settings.GetType()))
            {
                DynamicBoundAttribute[] attributes =
                    GetAttributes<DynamicBoundAttribute>(member);

                foreach (DynamicBoundAttribute attribute in attributes)
                {
                    if (!SatisfiesBound(settings, member, attribute))
                        return false;
                }
            }
            return true;
        }

        private static bool HasInvalidDynamicBounds(
            object settings,
            MemberInfo member)
        {
            DynamicBoundAttribute[] attributes =
                GetAttributes<DynamicBoundAttribute>(member);

            foreach (DynamicBoundAttribute attribute in attributes)
            {
                if (!SatisfiesBound(settings, member, attribute))
                    return true;
            }

            return false;
        }

        private static bool SatisfiesBound(
            object settings,
            MemberInfo member,
            DynamicBoundAttribute attribute)
        {
            object value = GetValue(member, settings);
            object boundValue = GetBoundValue(settings, attribute.memberName);

            if (value == null || boundValue == null)
                return true;

            if (value is not IComparable comparableValue)
                return true;

            int comparison = comparableValue.CompareTo(boundValue);

            return attribute.upper
                ? comparison <= 0
                : comparison >= 0;
        }

        private static object GetBoundValue(
            object settings,
            string memberName)
        {
            MemberInfo boundMember =
                GetMembers(settings.GetType())
                    .FirstOrDefault(m => m.Name == memberName);

            if (boundMember == null)
                return null;

            return GetValue(boundMember, settings);
        }

        private static bool IsNumeric(Type type)
        {
            return type == typeof(byte)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }
        private static IEnumerable<MemberInfo> GetMembers(Type type)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && property.CanWrite)
                    yield return property;
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!field.IsInitOnly)
                    yield return field;
            }
        }
        public static void CopyTo<T>(T source, T destination)
        {
            foreach (MemberInfo member in GetMembers(typeof(T)))
            {
                SetValue(
                    member,
                    destination,
                    GetValue(member, source));
            }
        }
        private static object GetValue(MemberInfo member, object instance)
        {
            return member switch
            {
                PropertyInfo property => property.GetValue(instance),
                FieldInfo field => field.GetValue(instance),
                _ => throw new NotSupportedException(
                    $"Unsupported member type {member.MemberType}")
            };
        }

        private static void SetValue(MemberInfo member, object instance, object value)
        {
            switch (member)
            {
                case PropertyInfo property:
                    property.SetValue(instance, value);
                    break;

                case FieldInfo field:
                    field.SetValue(instance, value);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported member type {member.MemberType}");
            }
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => throw new NotSupportedException(
                    $"Unsupported member type {member.MemberType}")
            };
        }

        private static T[] GetAttributes<T>(MemberInfo member)
            where T : Attribute
        {
            return member
                .GetCustomAttributes(typeof(T), true)
                .Cast<T>()
                .ToArray();
        }
        private void TrackEnforce(MemberInfo member, IReadOnlyList<string> path)
        {
            string name = string.Join(".", path.Append(member.Name));
            if (!LastStats.EnforcedMembers.Contains(name))
            {
                LastStats.EnforcedCount++;
                LastStats.EnforcedMembers.Add(name);
                ConnectionSettingsRando.Instance.Log($"Enforced {name}");
            }
        }
        private void TrackRando(MemberInfo member, IReadOnlyList<string> path)
        {
            string name = string.Join(".", path.Append(member.Name));
            if (!LastStats.RandomizedMembers.Contains(name))
            {
                LastStats.RandomizedCount++;
                LastStats.RandomizedMembers.Add(name);
                ConnectionSettingsRando.Instance.Log($"Randomized {name}");
            }
        }
        private void TrackSkip(MemberInfo member, IReadOnlyList<string> path)
        {
            LastStats.SkippedCount++;
            string name = string.Join(".", path.Append(member.Name));
            LastStats.SkippedMembers.Add(name);
            ConnectionSettingsRando.Instance.Log($"Skipped {name}");
        }
    }
}