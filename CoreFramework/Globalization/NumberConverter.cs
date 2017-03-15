using CoreFramework.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Globalization
{
    public enum TextCase { Nominative/*Кто? Что?*/, Genitive/*Кого? Чего?*/, Dative/*Кому? Чему?*/, Accusative/*Кого? Что?*/, Instrumental/*Кем? Чем?*/, Prepositional/*О ком? О чём?*/ };

    public static class RuDateAndMoneyConverter
    {
        static string[] monthNamesGenitive =
{
    "", "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" 
};

        static string zero = "ноль";
        static string firstMale = "один";
        static string firstFemale = "одна";
        static string firstFemaleAccusative = "одну";
        static string firstMaleGenetive = "одно";
        static string secondMale = "два";
        static string secondFemale = "две";
        static string secondMaleGenetive = "двух";
        static string secondFemaleGenetive = "двух";

        static string[] from3till19 = 
{
    "", "три", "четыре", "пять", "шесть",
    "семь", "восемь", "девять", "десять", "одиннадцать",
    "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
    "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
};
        static string[] from3till19Genetive = 
{
    "", "трех", "четырех", "пяти", "шести",
    "семи", "восеми", "девяти", "десяти", "одиннадцати",
    "двенадцати", "тринадцати", "четырнадцати", "пятнадцати",
    "шестнадцати", "семнадцати", "восемнадцати", "девятнадцати"
};
        static string[] tens =
{
    "", "двадцать", "тридцать", "сорок", "пятьдесят",
    "шестьдесят", "семьдесят", "восемьдесят", "девяносто"
};
        static string[] tensGenetive =
{
    "", "двадцати", "тридцати", "сорока", "пятидесяти",
    "шестидесяти", "семидесяти", "восьмидесяти", "девяноста"
};
        static string[] hundreds =
{
    "", "сто", "двести", "триста", "четыреста",
    "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"
};
        static string[] hundredsGenetive =
{
    "", "ста", "двухсот", "трехсот", "четырехсот",
    "пятисот", "шестисот", "семисот", "восемисот", "девятисот"
};
        static string[] thousands =
{
    "", "тысяча", "тысячи", "тысяч"
};
        static string[] thousandsAccusative =
{
    "", "тысячу", "тысячи", "тысяч"
};
        static string[] millions =
{
    "", "миллион", "миллиона", "миллионов"
};
        static string[] billions =
{
    "", "миллиард", "миллиарда", "миллиардов"
};
        static string[] trillions =
{
    "", "трилион", "трилиона", "триллионов"
};
//        static string[] rubles =
//{
//    "", "рубль", "рубля", "рублей"
//};
//        static string[] copecks =
//{
//    "", "копейка", "копейки", "копеек"
//};
        /// <summary>
        /// «07» января 2004 [+ _year(:года)]
        /// </summary>
        /// <param name="date"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static string DateToTextLong(DateTime date, string year)
        {
            return String.Format("«{0}» {1} {2}",
                                    date.Day.ToString("D2"),
                                    MonthName(date.Month, TextCase.Genitive),
                                    date.Year.ToString()) + ((year.Length != 0) ? " " : "") + year;
        }

        /// <summary>
        /// «07» января 2004
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string DateToTextLong(DateTime date)
        {
            return String.Format("«{0}» {1} {2}",
                                    date.Day.ToString("D2"),
                                    MonthName(date.Month, TextCase.Genitive),
                                    date.Year.ToString());
        }
        /// <summary>
        /// II квартал 2004
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string DateToTextQuarter(DateTime date)
        {
            return NumeralsRoman(DateQuarter(date)) + " квартал " + date.Year.ToString();
        }
        /// <summary>
        /// 07.01.2004
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string DateToTextSimple(DateTime date)
        {
            return String.Format("{0:dd.MM.yyyy}", date);
        }
        public static int DateQuarter(DateTime date)
        {
            return (date.Month - 1) / 3 + 1;
        }

        static int lastDigit(long _amount)
        {
            long amount = _amount;

            if (amount >= 100)
                amount = amount % 100;

            if (amount >= 20)
                amount = amount % 10;

            return (int)amount;
        }
        /// <summary>
        /// Десять тысяч рублей 67 копеек
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="firstCapital"></param>
        /// <returns></returns>
        public static string CurrencyToTxt(double amount, bool firstCapital, DigitItems rubles, DigitItems copecks)
        {
            //Десять тысяч рублей 67 копеек
            long rublesAmount = (long)Math.Floor(amount);
            long copecksAmount = ((long)Math.Round(amount * 100)) % 100;
            int lastRublesDigit = lastDigit(rublesAmount);
            int lastCopecksDigit = lastDigit(copecksAmount);

            string s = NumeralsToTxt(rublesAmount, TextCase.Nominative, true, firstCapital) + " " + 
                Declines.DeclineAny(lastRublesDigit, rubles) + " ";

            s += String.Format("{0:00} ", copecksAmount) + Declines.DeclineAny(lastCopecksDigit, copecks);

            return s.Trim();
        }
        /// <summary>
        /// 10 000 (Десять тысяч) рублей 67 копеек
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="firstCapital"></param>
        /// <returns></returns>
        public static string CurrencyToTxtFull(double amount, bool firstCapital, DigitItems rubles, DigitItems copecks)
        {
            //10 000 (Десять тысяч) рублей 67 копеек
            long rublesAmount = (long)Math.Floor(amount);
            long copecksAmount = ((long)Math.Round(amount * 100)) % 100;
            int lastRublesDigit = lastDigit(rublesAmount);
            int lastCopecksDigit = lastDigit(copecksAmount);

            string s = String.Format("{0:N0} ({1}) ", rublesAmount, NumeralsToTxt(rublesAmount, TextCase.Nominative, true, firstCapital)) +
                Declines.DeclineAny(lastRublesDigit, rubles) + " ";

            s += String.Format("{0:00} ", copecksAmount) + Declines.DeclineAny(lastCopecksDigit, copecks);

            return s.Trim();
        }
        /// <summary>
        /// 10 000 рублей 67 копеек  
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static string CurrencyToTxtShort(double amount, DigitItems rubles, DigitItems copecks)
        {
            //10 000 рублей 67 копеек        
            long rublesAmount = (long)Math.Floor(amount);
            long copecksAmount = ((long)Math.Round(amount * 100)) % 100;
            int lastRublesDigit = lastDigit(rublesAmount);
            int lastCopecksDigit = lastDigit(copecksAmount);

            string s = String.Format("{0:N0} ", rublesAmount) +
                Declines.DeclineAny(lastRublesDigit, rubles) + " ";

            s += String.Format("{0:00} ", copecksAmount) + Declines.DeclineAny(lastCopecksDigit, copecks);

            return s.Trim();
        }
        public static string MakeText(int digits, string[] hundreds, string[] tens, string[] from3till19, string second, string first, string[] power)
        {
            string s = "";

            if (digits >= 100)
            {
                s += hundreds[digits / 100] + " ";
                digits = digits % 100;
            }
            if (digits >= 20)
            {
                s += tens[digits / 10 - 1] + " ";
                digits = digits % 10;
            }

            if (digits >= 3)
            {
                s += from3till19[digits - 2] + " ";
            }
            else if (digits == 2)
            {
                s += second + " ";
            }
            else if (digits == 1)
            {
                s += first + " ";
            }

            if (digits != 0 && power.Length > 0)
            {
                digits = lastDigit(digits);

                if (Declines.IsPluralGenitive(digits))
                {
                    s += power[3] + " ";
                }
                else if (Declines.IsSingularGenitive(digits))
                {
                    s += power[2] + " ";
                }
                else
                {
                    s += power[1] + " ";
                }
            }

            return s;
        }

        /// <summary>
        /// реализовано для падежей: именительный (nominative), родительный (Genitive),  винительный (accusative)
        /// </summary>
        /// <param name="sourceNumber"></param>
        /// <param name="textcase"></param>
        /// <param name="isMale"></param>
        /// <param name="firstCapital"></param>
        /// <returns></returns>
        public static string NumeralsToTxt(long sourceNumber, TextCase textcase, bool isMale, bool firstCapital)
        {
            string s = "";
            long number = sourceNumber;
            int remainder;
            int power = 0;

            if ((number >= (long)Math.Pow(10, 15)) || number < 0)
            {
                return "";
            }

            while (number > 0)
            {
                remainder = (int)(number % 1000);
                number = number / 1000;

                switch (power)
                {
                    case 12:
                        s = MakeText(remainder, hundreds, tens, from3till19, secondMale, firstMale, trillions) + s;
                        break;
                    case 9:
                        s = MakeText(remainder, hundreds, tens, from3till19, secondMale, firstMale, billions) + s;
                        break;
                    case 6:
                        s = MakeText(remainder, hundreds, tens, from3till19, secondMale, firstMale, millions) + s;
                        break;
                    case 3:
                        switch (textcase)
                        {
                            case TextCase.Accusative:
                                s = MakeText(remainder, hundreds, tens, from3till19, secondFemale, firstFemaleAccusative, thousandsAccusative) + s;
                                break;
                            default:
                                s = MakeText(remainder, hundreds, tens, from3till19, secondFemale, firstFemale, thousands) + s;
                                break;
                        }
                        break;
                    default:
                        string[] powerArray = { };
                        switch (textcase)
                        {
                            case TextCase.Genitive:
                                s = MakeText(remainder, hundredsGenetive, tensGenetive, from3till19Genetive, isMale ? secondMaleGenetive : secondFemaleGenetive, isMale ? firstMaleGenetive : firstFemale, powerArray) + s;
                                break;
                            case TextCase.Accusative:
                                s = MakeText(remainder, hundreds, tens, from3till19, isMale ? secondMale : secondFemale, isMale ? firstMale : firstFemaleAccusative, powerArray) + s;
                                break;
                            default:
                                s = MakeText(remainder, hundreds, tens, from3till19, isMale ? secondMale : secondFemale, isMale ? firstMale : firstFemale, powerArray) + s;
                                break;
                        }
                        break;
                }

                power += 3;
            }

            if (sourceNumber == 0)
            {
                s = zero + " ";
            }

            if (s != "" && firstCapital)
                s = s.Substring(0, 1).ToUpper() + s.Substring(1);

            return s.Trim();
        }
        public static string NumeralsDoubleToTxt(double sourceNumber, int _decimal, TextCase textcase, bool firstCapital)
        {
            long decNum = (long)Math.Round(sourceNumber * Math.Pow(10, _decimal)) % (long)(Math.Pow(10, _decimal));

            string s = String.Format(" {0} целых {1} сотых", NumeralsToTxt((long)sourceNumber, textcase, true, firstCapital),
                                                  NumeralsToTxt((long)decNum, textcase, true, false));
            return s.Trim();
        }
        /// <summary>
        /// название м-ца
        /// </summary>
        /// <param name="month">с единицы</param>
        /// <param name="textcase"></param>
        /// <returns></returns>
        public static string MonthName(int month, TextCase textcase)
        {
            string s = "";

            if (month > 0 && month <= 12)
            {
                switch (textcase)
                {
                    case TextCase.Genitive:
                        s = monthNamesGenitive[month];
                        break;
                    default:
                        throw new NotImplementedException(textcase.ToString());
                }
            }

            return s;
        }
        public static string NumeralsRoman(int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + NumeralsRoman(number - 1000);
            if (number >= 900) return "CM" + NumeralsRoman(number - 900); //EDIT: i've typed 400 instead 900
            if (number >= 500) return "D" + NumeralsRoman(number - 500);
            if (number >= 400) return "CD" + NumeralsRoman(number - 400);
            if (number >= 100) return "C" + NumeralsRoman(number - 100);
            if (number >= 90) return "XC" + NumeralsRoman(number - 90);
            if (number >= 50) return "L" + NumeralsRoman(number - 50);
            if (number >= 40) return "XL" + NumeralsRoman(number - 40);
            if (number >= 10) return "X" + NumeralsRoman(number - 10);
            if (number >= 9) return "IX" + NumeralsRoman(number - 9);
            if (number >= 5) return "V" + NumeralsRoman(number - 5);
            if (number >= 4) return "IV" + NumeralsRoman(number - 4);
            if (number >= 1) return "I" + NumeralsRoman(number - 1);
            throw new ArgumentOutOfRangeException("something bad happened");
        }
    }
}
