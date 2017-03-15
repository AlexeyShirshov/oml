using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace CoreFramework.Config
{
    public class DecimalValidator : ConfigurationValidatorBase
    {
        public decimal MinValue { get; private set; } = decimal.MinValue;
        public decimal MaxValue { get; private set; } = decimal.MaxValue;
        public DecimalValidator() { }
        public DecimalValidator(decimal minValue, decimal maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override bool CanValidate(Type type)
        {
            return type == typeof(decimal);
        }

        public override void Validate(object value)
        {
            if (value == null)
                return;

            decimal res = Convert.ToDecimal(value);

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


    public class DecimalValidatorAttribute : ConfigurationValidatorAttribute
    {
        public decimal MinValue { get; set; } = decimal.MinValue;
        public decimal MaxValue { get; set; } = decimal.MaxValue;
        public DecimalValidatorAttribute() { }
        public DecimalValidatorAttribute(decimal minValue, decimal maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override ConfigurationValidatorBase ValidatorInstance => new DecimalValidator(MinValue, MaxValue);
    }
}
