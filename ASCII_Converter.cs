using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNGHandler
{
   public enum GREYSCALE_CONVERSION_VALUES { PURE_BLACK = 80, SEMI_BLACK = 120, GREY = 180, WHITE = 255};
   public enum Image_Sizes { ICON = 20, SMALL = 100, MEDIUM = 400, LARGE = 1000, X_LARGE = 2000};
   public enum Hash_Widths { ICON = 1, SMALL = 3, MEDIUM = 6, LARGE = 12, X_LARGE = 24};
   public enum Hash_Heights { ICON = 1, SMALL = 5, MEDIUM = 12, LARGE = 24, X_LARGE = 48};

   class ASCII_Converter
   {
      static public string[] convert_to_ASCII_art(byte[] greyscale_bytes, int width, int height)
      {
         Image_Sizes image_size = calculate_image_tier(width);

         if (image_tier == null)
         {
            image_tier = image_size;
         }

         if(initialized == null
            || !initialized
            || (initialized && image_size != image_tier))
         {
            initialize_ASCII_hashes(image_size);
         }

         // convert the byte array into a two dimensional array of bytes
         byte[][] grey_bytes = new byte[height][];
         int byte_index = 0;
         for(int i = 0; i < height; i++){
            grey_bytes[i] = new byte[width];
            for(int j = 0; j < width; j++){
               grey_bytes[i][j] = greyscale_bytes[byte_index++];
            }
         }

         // get the ByteGrid of the greyscale bytes
         int remaining_height;
         int remaining_width;
         ByteGrid[][] byte_grid_grid = create_byte_grid_grid(grey_bytes, out remaining_height, out remaining_width);

         // get the hashes for all the pixel grids
         uint[][] hashes = hash_byte_grid(byte_grid_grid);

         // convert all hashes to strings
         // ERROR TESTING
         // make it so that if an image is very small, it uses bare bones,
         // if it's small to x_large, use all hashes, but pass in the correct
         //    folder to take the ASCII pics from
         
         //string[] ASCII_lines = null;
         //if (image_tier == Image_Sizes.ICON)
         //{
         //   ASCII_lines = shade_ASCII(grey_bytes);
         //}
         //else
         //{
         //   ASCII_lines = all_hashes_to_ASCII(hashes);
         //}
         //string[] ASCII_lines = bare_bones_ASCII(grey_bytes);

         string[] ASCII_lines = shade_ASCII(grey_bytes);

         return ASCII_lines;
      }

            static private ByteGrid[][] create_byte_grid_grid(byte[][] greyscale_bytes, out int remaining_height, out int remaining_width)
            {
               //if (!is_rectangular_2D_array(greyscale_bytes, 5, 5))
               //{
               //   remaining_height = 0;
               //   remaining_width = 0;
               //   return null;
               //}

               int data_height = greyscale_bytes.Length;
               int data_width = greyscale_bytes[0].Length;

               int hash_height = calculate_hash_height();
               int hash_width = calculate_hash_width();

               int result_height = data_height / hash_height;
               int result_width = data_width / hash_width;

               remaining_height = data_height % hash_height;
               remaining_width = data_width % hash_width;

               // account for indivisible picture dimensions
               if (remaining_height > 0)
               {
                  result_height++;
               }
               if (remaining_width > 0)
               {
                  result_width++;
               }

               // get the bytegrids
               List<ByteGrid> grid_list = new List<ByteGrid>();
               for(int grid_height_index = 0; grid_height_index < result_height; grid_height_index++){
                  for(int grid_width_index = 0; grid_width_index < result_width; grid_width_index++){
                     // in the correct grid area of the 2D byte array,
                     // now copy the appropriate bytes over to the
                     // temp byte[][], make a ByteGrid out of them, and
                     // push it into the list

                     // get the starting, top-left corner of the 2-D byte array you're going to convert to a ByteGrid
                     int byte_height_index = grid_height_index * hash_height;
                     
                     byte[][] grid_arr = new byte[hash_height][];

                     for(int i = 0; i < hash_height; i++){
                        grid_arr[i] = new byte[hash_width];
                        int byte_width_index = grid_width_index * hash_width;

                        for(int j = 0; j < hash_width; j++){
                           if (byte_height_index >= greyscale_bytes.Length)
                           {
                              grid_arr[i][j] = 255; // make the unexisting bit white
                           }
                           else if (byte_width_index >= greyscale_bytes[byte_height_index].Length)
                           {
                              grid_arr[i][j] = 255; // make the unexisting bit white
                           }
                           else
                           {
                              grid_arr[i][j] = greyscale_bytes[byte_height_index][byte_width_index];
                           }
                           ++byte_width_index;
                        }
                        byte_height_index++;
                     }

                     grid_list.Add(new ByteGrid(grid_arr));
                  }
               }

               // push the bytegrids into the result array and return it.
               // prepare the return list
               ByteGrid[][] result = new ByteGrid[result_height][];
               for (int i = 0; i < result_height; i++)
               {
                  result[i] = new ByteGrid[result_width];
               }
               int result_height_index = 0;
               int result_width_index = 0;
               foreach (ByteGrid grid in grid_list)
               {
                  result[result_height_index][result_width_index] = grid;
                  ++result_width_index;
                  if (result_width_index % result_width == 0)
                  {
                     ++result_height_index;
                     result_width_index = 0;
                  }
               }

               return result;
            }

                  static private bool is_rectangular_2D_array(byte[][] arr, int expected_height, int expected_width){
                     bool null_argument = arr == null;
                     if (arr.Length != expected_height)
                     {
                        throw new ArgumentException("ASCII_Converter: Pixel hashing failed. Invalid height.");
                     }
                     for (int i = 0; i < arr.Length; i++)
                     {
                        null_argument = null_argument || arr[i] == null;
                        if (null_argument)
                        {
                           throw new ArgumentNullException("ASCII_Converter: Pixel hashing failed. Invalid width.");
                        }
                        else if (arr[i].Length != expected_width)
                        {
                           throw new ArgumentException("ASCII_Converter: Pixel hashing failed.");
                        }
                     }
                     return !null_argument;
                  }

                  static private int calculate_hash_height()
                  {
                     if (image_tier == Image_Sizes.ICON)
                     {
                        return (int)Hash_Heights.ICON;
                     }
                     else if (image_tier == Image_Sizes.SMALL)
                     {
                        return (int)Hash_Heights.SMALL;
                     }
                     else if (image_tier == Image_Sizes.MEDIUM)
                     {
                        return (int)Hash_Heights.MEDIUM;
                     }
                     else if (image_tier == Image_Sizes.LARGE)
                     {
                        return (int)Hash_Heights.LARGE;
                     }
                     else
                     {
                        return (int)Hash_Heights.X_LARGE;
                     }
                  }

                  static private int calculate_hash_width()
                  {
                     if (image_tier == Image_Sizes.ICON)
                     {
                        return (int)Hash_Widths.ICON;
                     }
                     else if (image_tier == Image_Sizes.SMALL)
                     {
                        return (int)Hash_Widths.SMALL;
                     }
                     else if (image_tier == Image_Sizes.MEDIUM)
                     {
                        return (int)Hash_Widths.MEDIUM;
                     }
                     else if (image_tier == Image_Sizes.LARGE)
                     {
                        return (int)Hash_Widths.LARGE;
                     }
                     else
                     {
                        return (int)Hash_Widths.X_LARGE;
                     }
                  }

            static private uint[][] hash_byte_grid(ByteGrid[][] byte_grid)
            {
               if (byte_grid == null
                  || (byte_grid.Length > 0
                      && byte_grid[0] == null))
               {
                  throw new ArgumentNullException("ASCII_Converter: Unable to create hash.");
               }
               // takes an n by n array of greyscale pixels
               // and converts them to a hash
               uint[][] hash_grid = new uint[byte_grid.Length][];

               for (int i = 0; i < byte_grid.Length; i++)
               {
                  hash_grid[i] = new uint[byte_grid[i].Length];

                  for (int j = 0; j < byte_grid[0].Length; j++)
                  {
                     hash_grid[i][j] = byte_grid_to_pixel_hash(byte_grid[i][j]);
                  }
               }
               
               return hash_grid;
            }

                  static private uint byte_grid_to_pixel_hash(ByteGrid grid){
                     if(grid == null
                        || !grid.valid_grid){
                        throw new ArgumentNullException("ASCII_Converter: Null ByteGrid object passed when trying to hash the pixel value.");
                     }

                     uint hash = 0;
                     // 2 bits per byte

                     for(int i = 0; i < grid.height; i++){
                        for(int j = 0; j < grid.width; j++){
                           hash = hash << 2;

                           GREYSCALE_CONVERSION_VALUES conv_val =
                              (GREYSCALE_CONVERSION_VALUES)grid.grid[i][j];

                           if (conv_val <= GREYSCALE_CONVERSION_VALUES.PURE_BLACK)
                           {
                              hash += 3;
                           }
                           else if (conv_val <= GREYSCALE_CONVERSION_VALUES.SEMI_BLACK)
                           {
                              hash += 2;
                           }
                           else if (conv_val <= GREYSCALE_CONVERSION_VALUES.GREY)
                           {
                              hash += 1;
                           }
                           else if (conv_val <= GREYSCALE_CONVERSION_VALUES.WHITE)
                           {
                              hash += 0;
                           }
                           else
                           {
                              throw new InvalidOperationException("ASCII_Converter: Invalid greyscale byte passed.");
                           }
                        }
                     }

                     return hash;
                  }

            static private Image_Sizes calculate_image_tier(int width)
            {
               Image_Sizes image_tier;

               if ((Image_Sizes)width < Image_Sizes.SMALL)
               {
                  image_tier = Image_Sizes.ICON;
               }
               else if ((Image_Sizes)width < Image_Sizes.MEDIUM)
               {
                  image_tier = Image_Sizes.SMALL;
               }
               else if ((Image_Sizes)width < Image_Sizes.LARGE)
               {
                  image_tier = Image_Sizes.MEDIUM;
               }
               else if ((Image_Sizes)width < Image_Sizes.X_LARGE)
               {
                  image_tier = Image_Sizes.LARGE;
               }
               else
               {
                  image_tier = Image_Sizes.X_LARGE;
               }

               return image_tier;
            }

            static private string[] all_hashes_to_ASCII(uint[][] hash_grid) {
               if(hash_grid == null
                  || hash_grid.Length == 0)
               {
                  throw new ArgumentNullException("ASCII_Converter: Null hashes passed to hashing algorithm.");
               }
               
               StringBuilder builder = new StringBuilder();
               string[] result = new string[hash_grid.Length];

               for (int i = 0; i < hash_grid.Length; i++)
               {
                  builder.Clear();
                  for (int j = 0; j < hash_grid[0].Length; j++)
                  {
                     builder.Append(hash_to_ASCII(hash_grid[i][j]));
                  }

                  result[i] = builder.ToString();
               }
               
               return result;
            }

                  static private char hash_to_ASCII(uint hash)
                  {
                     char best_char = ' ';
                     int best_bit_matches = 0;
                     // compare the hashes to the ascii hashes

                     for (int i = 0; i < ASCII_hashes.Length; i++)
                     {
                        int n_bit_matches = 0;
                        uint temp_hash = hash;

                        uint comparison_hash = ~(hash ^ ASCII_hashes[i]);

                        while (comparison_hash > 0
                           && temp_hash > 0)
                        {
                           if ((comparison_hash & 1) == 1)
                           {
                              ++n_bit_matches;
                           }
                           comparison_hash = comparison_hash >> 1;
                           temp_hash = temp_hash >> 1;
                        }

                        if (n_bit_matches > best_bit_matches)
                        {
                           best_bit_matches = n_bit_matches;
                           best_char = ASCII_Characters[i];
                        }
                     }

                     return best_char;
                  }

            /// <summary>
            /// A testing method to see if the bytes are coming out correctly.
            /// Simply translates any non-white pixel to '1'.
            /// </summary>
            /// <param name="grey_bytes"></param>
            /// <returns></returns>
            static private string[] bare_bones_ASCII(byte[][] grey_bytes)
            {
               string[] result = new string[grey_bytes.Length];

               StringBuilder builder = new StringBuilder();

               for (int i = 0; i < grey_bytes.Length; i++)
               {
                  builder.Clear();

                  for (int j = 0; j < grey_bytes[0].Length; j++)
                  {
                     if (grey_bytes[i][j] == 255)
                     {
                        builder.Append("  ");
                     }
                     else
                     {
                        builder.Append("11");
                     }
                  }

                  result[i] = builder.ToString();
               }

               return result;
            }

            static private string[] shade_ASCII(byte[][] grey_bytes)
            {
               string[] result = new string[grey_bytes.Length];
               StringBuilder builder = new StringBuilder();

               for (int i = 0; i < grey_bytes.Length; i++)
               {
                  builder.Clear();
                  for (int j = 0; j < grey_bytes[0].Length; j++)
                  {
                     GREYSCALE_CONVERSION_VALUES conv_val = (GREYSCALE_CONVERSION_VALUES)grey_bytes[i][j];
                     if (conv_val <= GREYSCALE_CONVERSION_VALUES.PURE_BLACK)
                     {
                        builder.Append("##");
                     }
                     else if (conv_val <= GREYSCALE_CONVERSION_VALUES.SEMI_BLACK)
                     {
                        builder.Append("==");
                     }
                     else if (conv_val <= GREYSCALE_CONVERSION_VALUES.GREY)
                     {
                        builder.Append("~~");
                     }
                     else
                     {
                        builder.Append("  ");
                     }
                  }
                  result[i] = builder.ToString();
               }
               
               return result;
            }

            static public void initialize_ASCII_hashes(Image_Sizes image_size){
               // get the folder
               StringBuilder path_builder = new StringBuilder(System.Environment.CurrentDirectory);

               if (image_size == Image_Sizes.SMALL)
               {
                  path_builder.Append("\\ASCII_Characters_Small\\");
               }
               else if (image_size == Image_Sizes.MEDIUM)
               {
                  path_builder.Append("\\ASCII_Characters_Medium\\");
               }
               else if (image_size == Image_Sizes.LARGE)
               {
                  path_builder.Append("\\ASCII_Characters_Large\\");
               }
               else if (image_size == Image_Sizes.X_LARGE)
               {
                  path_builder.Append("\\ASCII_Characters_X_Large\\");
               }
               else
               {
                  throw new ArgumentException("ASCII_Converter: Invalid image size for ASCII hash initialization.");
               }

               // record the image tier
               image_tier = image_size;

               ASCII_Characters = new char[] {'!', '"', '#', '$', '%', '&', '\'', '(', ')', '*',
                  '+', ',', '-', '.', '/', '0', '1', '2', '3', '4',
                  '5', '6', '7', '8', '9', ':', ';', '<', '=', '>',
                  '?', '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
                  'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R',
                  'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\',
                  ']', '^', '_', '`', 'a', 'b', 'c', 'd', 'e', 'f',
                  'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                  'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                  ' ', '{', '|', '}'
               };
               string[] files = {"exclamation_mark", "quotation_mark", "hashtag", "dollar_sign", "percent", "ampersans", "apostrophe", "left_parenthesis", "right_parenthesis", "asterisk",
                                "plus_sign", "comma", "minus_sign", "period", "forward_slash", "zero", "one", "two", "three", "four",
                                "five", "six", "seven", "eight", "nine", "colon", "semicolon", "less_than_symbol", "equals_sign", "greater_than_symbol",
                                "question_mark", "at_symbol", "A", "B", "C", "D", "E", "F", "G", "H",
                                "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R",
                                "S", "T", "U", "V", "W", "X", "Y", "Z", "left_square_bracket", "backslash",
                                "right_square_bracket", "carrot", "underscore", "single_quote_mark", "lowercase_a", "lowercase_b", "lowercase_c", "lowercase_d", "lowercase_e", "lowercase_f",
                                "lowercase_g", "lowercase_h", "lowercase_i", "lowercase_j", "lowercase_k", "lowercase_l", "lowercase_m", "lowercase_n", "lowercase_o", "lowercase_p",
                                "lowercase_q", "lowercase_r", "lowercase_s", "lowercase_t", "lowercase_u", "lowercase_v", "lowercase_w", "lowercase_x", "lowercase_y", "lowercase_z",
                                "space", "left_bracket", "vertical_line", "right_bracket"
                                };
               ASCII_hashes = new uint[ASCII_Characters.Length];

               for(int i = 0; i < ASCII_Characters.Length; i++){
                  BinaryFileReader file_reader = new BinaryFileReader(files[i] + ".png", path_builder.ToString());
                  PNG_Decoder png_decoder = new PNG_Decoder(file_reader.file_data);
                  byte[] file_bytes = png_decoder.to_greyscale();

                  for(int byte_index = 0; byte_index < file_bytes.Length; byte_index++){
                     ASCII_hashes[i] = ASCII_hashes[i] << 1;
                     if (file_bytes[byte_index] < 100)
                     {
                        ASCII_hashes[i] += 1;
                     }
                     //ASCII_hashes[i] += file_bytes[byte_index];
                  }
               }

               initialized = true;
            }

            

      static public void to_txt(string[] ASCII_strings, string filepath)
      {
         System.IO.StreamWriter writer = System.IO.File.CreateText(filepath);

         foreach (string ASCII_string in ASCII_strings)
         {
            writer.WriteLine(ASCII_string);
         }

         //writer.Flush();
         writer.Close();
      }

      static char[] ASCII_Characters;
      static private uint[] ASCII_hashes;
      static bool initialized;
      static private Image_Sizes image_tier;
   }
}
