using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNGHandler
{
   public enum CRC_Modes { CRC_1, 
      CRC_4_ITU, 
      CRC_5_EPC, 
      CRC_5_ITU, 
      CRC_5_USB, 
      CRC_6_CDMA2000_A, 
      CRC_6_CDMA2000_B, 
      CRC_6_ITU, 
      CRC_7, 
      CRC_7_MVB, 
      CRC_8, 
      CRC_8_CCITT, 
      CRC_8_DALLAS_MAXIM, 
      CRC_8_SAE_J1850, 
      CRC_8_WCDMA, 
      CRC_10, 
      CRC_10_CDMA2000, 
      CRC_11, 
      CRC_12, 
      CRC_12_CDMA2000, 
      CRC_13_BBC, 
      CRC_15_CAN, 
      CRC_15_MPT1327, 
      Chakravarty, 
      CRC_16_ARINC, 
      CRC_16_CCITT, 
      CRC_16_CDMA2000, 
      CRC_16_DECT, 
      CRC_16_T10_DIF, 
      CRC_16_DNP, 
      CRC_16_IBM, 
      Fletcher, 
      CRC_17_CAN, 
      CRC_21_CAN, 
      CRC_24, 
      CRC_24_Radix_64, 
      CRC_30, 
      Adler_32, 
      CRC_32, 
      CRC_32C, 
      CRC_32K, 
      CRC_32Q, 
      CRC_40_GSM, 
      CRC_64_ECMA, 
      CRC_64_ISO};
   
   class CRC_Handler
   {
      /// <summary>Testing mainframe for the CRC_Handler class.</summary>
      /*
      static public void Main(String[] args)
      {
         CRC_Handler handler = new CRC_Handler(CRC_Modes.CRC_32);

         Console.WriteLine("What are the CRC values?:");
         for (int i = 0; i < 256; i++)
         {
            ulong crc = handler.get_CRC(new byte[1] { (byte)i });
            Console.WriteLine("\t#{0})\t{1}", i + 1, crc);
            Console.WriteLine("\t\tHex: {0}", Hexadecimal.ToHex((uint)crc));
         }

         Console.ReadKey();
         Console.ReadKey();
      }
      */

      /// <summary>
      /// AREAS OF IMPROVEMENT: Expand the class to include all the CRC
      /// calculation methods. </summary>
      /// <param name="mode">The CRC calculation mode.</param>
      public CRC_Handler(CRC_Modes mode)
      {
         // Calculate the CRCs for the given CRC mode.
         // For the time being, it can only be CRC32
         this.mode = mode;
         CRC_32();
      }

            /// <summary>
            /// A constructor helper specialized to initialize and fill the CRC
            /// table for fast CRC_32 lookup.
            /// </summary>
            private void CRC_32()
            {
               CRCs_p = new ulong[256];

               ulong c;
               int n, k;

               for (n = 0; n < 256; n++)
               {
                  c = (ulong)n;
                  for (k = 0; k < 8; k++)
                  {
                     if ((c & 1) != 0)
                     {
                        c = 0xedb88320 ^ (c >> 1);
                     }
                     else
                     {
                        c = c >> 1;
                     }
                  }
                  CRCs_p[n] = c;
               }
            }


      // Methods
      public ulong get_CRC(byte[] CRC_bytes)
      {
         ulong original_crc = 0;

         if(mode == CRC_Modes.CRC_32){
            original_crc = 0xFFFFFFFFL;
         }

         ulong result = update_crc(original_crc, CRC_bytes);

         return result;
      }

            /// <summary>
            /// Helper method to properly calculate the correct CRC.
            /// </summary>
            /// <param name="original_crc"></param>
            /// <param name="CRC_bytes"></param>
            /// <returns></returns>
            private ulong update_crc(ulong original_crc, byte[] CRC_bytes)
            {
               ulong c = original_crc;
               
               for (int n = 0; n < CRC_bytes.Length; n++)
               {
                  c = CRCs_p[(c ^ CRC_bytes[n]) & 0xff] ^ (c >> 8);
               }

               return c;
            }

      // Fields
      private ulong[] CRCs_p;
      private CRC_Modes mode;
   }
}
