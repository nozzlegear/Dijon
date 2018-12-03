namespace Dijon.Environment
{
    public class EnvironmentVariable
    {
        public EnvironmentVariable(string varName)
        {
            var varValue = System.Environment.GetEnvironmentVariable(varName);

            if (string.IsNullOrEmpty(varValue))
            {
                HasValue = false;
            }
            else
            {
                HasValue = true;
                Value = varValue;
            }
        }

        public bool HasValue { get; }

        public string Value { get; }
    }
}