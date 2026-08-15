using MenuChanger.Attributes;

namespace ConnectionSettingsRando
{
    public class TestSettings
    {
        public bool BoolField;

        [MenuRange(1, 10)]
        public int IntField;

        public TestEnum EnumField;

        public bool BoolProperty { get; set; }

        [MenuRange(0f, 1f)]
        public float FloatProperty { get; set; }
    }
    public enum TestEnum
    {
        Three = 3,
        Two = 2,
        One = 1,
        Go = 0
    }
}