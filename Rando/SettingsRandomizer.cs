using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MenuChanger.Attributes;

namespace ConnectionSettingsRando
{
    public class RandomizationStats
    {
        public int RandomizedMembers { get; set; }
        public int SkippedMembers { get; set; }
    }
    public class SettingsRandomizer
    {
        public RandomizationStats LastStats { get; private set; } = new();
        public T Randomize<T>(T settings, Random rng)
            where T : new()
        {
            LastStats = new();
            T clone = Clone(settings);

            foreach (MemberInfo member in GetMembers(typeof(T)))
            {
                object value = GetValue(member, clone);

                object randomized = RandomizeValue(
                    member,
                    value,
                    rng);

                SetValue(member, clone, randomized);
            }

            return clone;
        }

        public class NumericRange
        {
            public double Min { get; }
            public double Max { get; }

            public NumericRange(double min, double max)
            {
                Min = min;
                Max = max;
            }
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

        private static TAttribute GetAttribute<TAttribute>(MemberInfo member)
            where TAttribute : Attribute
        {
            return member.GetCustomAttribute<TAttribute>();
        }

        private static bool HasConstraints(MemberInfo member)
        {   
            return member.GetCustomAttribute(typeof(DynamicBoundAttribute)) != null;
        }

        private object RandomizeValue(
            MemberInfo member,
            object value,
            Random rng)
        {
            Type type = GetMemberType(member);
            if (HasConstraints(member))
            {
                LastStats.SkippedMembers++;
                ConnectionSettingsRando.Instance.Log($"Skipped {member.Name} - reason: HasConstraints.");
                return value;
            }
            if (type == typeof(bool))
            {
                if (RandoInterop.Settings.IncludeBooleans)
                {
                    LastStats.RandomizedMembers++;
                    return RandomizeBool(rng);
                }
                else
                {
                    OptOut(member.Name);
                    return value;
                }
            }

            if (IsNumeric(type))
            {
                if (RandoInterop.Settings.IncludeNumeric)
                {
                    LastStats.RandomizedMembers++;
                    return RandomizeNumeric(member, rng);
                }
                else
                {
                    OptOut(member.Name);
                    return value;
                }
            }

            if (type.IsEnum)
            {
                if (RandoInterop.Settings.IncludeCategorical)
                {
                    LastStats.RandomizedMembers++;
                    return RandomizeEnum(type, rng);
                }
                else
                {
                    OptOut(member.Name);
                    return value;
                }
            }
            
            if (!type.IsPrimitive && value != null)
            {
                LastStats.RandomizedMembers++;
                return RandomizeObject(value, rng);
            }

            LastStats.SkippedMembers++;
            ConnectionSettingsRando.Instance.Log($"Skipped {member.Name} - reason: Unhandled setting.");
            return value;
        }

        private void OptOut(string name)
        {
            LastStats.SkippedMembers++;
            ConnectionSettingsRando.Instance.Log($"Skipped {name} - reason: Opted out.");
        }

        private object RandomizeBool(Random rng)
        {
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
                    int min = Convert.ToInt32(range?.min ?? 0);
                    int max = Convert.ToInt32(range?.max ?? 100);

                    return rng.Next(min, max + 1);
                }

                case TypeCode.Int64:
                {
                    long min = Convert.ToInt64(range?.min ?? 0L);
                    long max = Convert.ToInt64(range?.max ?? 100L);

                    // Random doesn't support long directly
                    return min + (long)(rng.NextDouble() * (max - min + 1));
                }

                case TypeCode.Single:
                {
                    float min = Convert.ToSingle(range?.min ?? 0f);
                    float max = Convert.ToSingle(range?.max ?? 1f);

                    return (float)(min + rng.NextDouble() * (max - min));
                }

                case TypeCode.Double:
                {
                    double min = Convert.ToDouble(range?.min ?? 0d);
                    double max = Convert.ToDouble(range?.max ?? 1d);

                    return min + rng.NextDouble() * (max - min);
                }

                case TypeCode.Decimal:
                {
                    decimal min = Convert.ToDecimal(range?.min ?? 0m);
                    decimal max = Convert.ToDecimal(range?.max ?? 1m);

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
        private object RandomizeObject(
            object instance,
            Random rng)
        {
            Type type = instance.GetType();
            if (!IsSettingsObject(type))
                return instance;

            object clone = Activator.CreateInstance(type)!;
            foreach (MemberInfo member in GetMembers(type))
            {
                object value = GetValue(member, instance);

                object randomized = RandomizeValue(
                    member,
                    value,
                    rng);

                SetValue(
                    member,
                    clone,
                    randomized);
            }

            return clone;
        }

        private (double min, double max) GetNumericBounds(
            MemberInfo member,
            object instance)
        {
            var range = GetAttribute<MenuRangeAttribute>(member);

            double min = Convert.ToDouble(range?.min ?? 0);
            double max = Convert.ToDouble(range?.max ?? 1);

            var dynamicBound =
                GetAttribute<DynamicBoundAttribute>(member);

            if (dynamicBound != null)
            {
                MemberInfo boundMember =
                    GetMembers(instance.GetType())
                        .FirstOrDefault(x =>
                            x.Name == dynamicBound.memberName);

                if (boundMember != null)
                {
                    object boundValue =
                        GetValue(boundMember, instance);

                    double bound =
                        Convert.ToDouble(boundValue);

                    if (dynamicBound.upper)
                        min = Math.Min(max, bound);
                    else
                        max = Math.Max(min, bound);
                }
            }

            return (min, max);
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
    }
}