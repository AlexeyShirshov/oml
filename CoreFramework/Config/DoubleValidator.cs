using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace CoreFramework.Config
{
    public class DoubleValidator : ConfigurationValidatorBase
    {
        public double MinValue { get; private set; } = double.MinValue;
        public double MaxValue { get; private set; } = double.MaxValue;
        public DoubleValidator() { }
        public DoubleValidator(double minValue, double maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override bool CanValidate(Type type)
        {
            return type == typeof(double);
        }

        public override void Validate(object value)
        {
            if (value == null)
                return;

            double res = Convert.ToDouble(value);

            if (res < MinValue)
            {
                throw new ConfigurationErrorsException($"Value too low, minimum value allowed: {MinValue}");
            }

            if (res > MaxValue)
            {
                throw new ConfigurationErrorsException($"Value too high, maximum value allowed: {MaxValue}");
            }
        }
    }

    public class DoubleValidatorAttribute : ConfigurationValidatorAttribute
    {
        public double MinValue { get; set; } = double.MinValue;
        public double MaxValue { get; set; } = double.MaxValue;
        public DoubleValidatorAttribute() { }
        public DoubleValidatorAttribute(double minValue, double maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override ConfigurationValidatorBase ValidatorInstance => new DoubleValidator(MinValue, MaxValue);
    }
}
