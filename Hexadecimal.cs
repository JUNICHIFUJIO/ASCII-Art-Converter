using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNGHandler
{
   class Hexadecimal
   {
      static public string ToHex(uint n)
      {
         if (n == 0)
         {
            return "0x00000000";
         }

         StringBuilder result = new StringBuilder("0x");

         uint hex = uint.MaxValue / 16 + 1;

         uint val = 0;
         if (n > hex)
         {
            val = n / 16;
         }
         result.Append(ToHexDigit(val));

         while (hex / 16 > n)
         {
            result.Append('0');
            hex /= 16;
         }

         while (n >= 16)
         {
            val = n / 16;
            result.Append(ToHexDigit(val));

            hex /= 16;
            n -= val * hex;
         }

         val = n % 16;
         result.Append(ToHexDigit(val));

         return result.ToString();
      }

      static public char ToHexDigit(uint n)
      {
         switch (n)
         {
            case (10):
               return 'A';
            case (11):
               return 'B';
            case (12):
               return 'C';
            case (13):
               return 'D';
            case (14):
               return 'E';
            case (15):
               return 'F';
         }

         return n.ToString()[0];
      }
   }
}
