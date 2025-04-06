namespace AspCoreApi.Ekstensi
{
  
        public static class StringExtensions
        {
            public static string Fa2En(this string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return input;

                var persianDigits = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };

                for (int i = 0; i < persianDigits.Length; i++)
                {
                    input = input.Replace(persianDigits[i], i.ToString()[0]);
                }

                return input;
            }

            public static string FixPersianChars(this string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return input;

                return input
                    .Replace('ي', 'ی') // Arabic yeh to Persian yeh
                    .Replace('ك', 'ک') // Arabic ke to Persian ke
                    .Replace("‌", " ") // Remove zero-width non-joiner
                    .Trim();
            }
        }
   

}
