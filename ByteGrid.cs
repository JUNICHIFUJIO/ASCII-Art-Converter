using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNGHandler
{
   class ByteGrid
   {
      public ByteGrid(byte[][] byte_arr){
         grid = byte_arr;
      }

      public ByteGrid(int height, int width)
      {
         if (height < 1
            || width < 1)
         {
            grid = null;
         }
         else
         {
            grid = new byte[height][];
            for (int i = 0; i < width; i++)
            {
               grid[i] = new byte[width];
            }
         }
      }

      public int height
      {
         get
         {
            if (grid == null)
            {
               return 0;
            }
            else
            {
               return grid.Length;
            }
         }
         set
         {
            if (value > 0)
            {
               // handle null grids
               if (!valid_grid)
               {
                  grid = new byte[value][];
                  for (int i = 0; i < value; i++)
                  {
                     grid[i] = new byte[1];
                  }
               }
               
               // warn of loss of data
               if (height > value)
               {
                  Console.WriteLine("Warning! Loss of data when reassigning ByteGrid's height!");
               }

               // if the value is the same as the height already, don't do anything
               if (value != height)
               {
                  // create a new grid
                  byte[][] temp_grid = new byte[value][];
                  int lower_height = value;
                  if (lower_height > height)
                  {
                     lower_height = height;
                  }
                  // copy values over
                  for (int i = 0; i < lower_height; i++)
                  {
                     temp_grid[i] = new byte[width];
                     for (int j = 0; j < width; j++)
                     {
                        temp_grid[i][j] = grid[i][j];
                     }
                  }

                  // reassign grid
                  grid = temp_grid;
               }
            }
            else
            {
               throw new ArgumentException("ByteGrid: Invalid height parameter. Must be greater than 0.");
            }
         }
      }
      public int width
      {
         get
         {
            if (grid == null
               || grid[0] == null)
            {
               return 0;
            }
            else
            {
               return grid[0].Length;
            }
         }
         set
         {
            if (value > 0)
            {
               // handle null grids
               if (!valid_grid)
               {
                  grid = new byte[1][];
                  grid[0] = new byte[value];
               }

               // warn of loss of data
               if (width > value)
               {
                  Console.WriteLine("Warning! Loss of data when reassigning ByteGrid's width!");
               }

               // 
               // if the value is the same as the width already, don't do anything
               if (value != width)
               {
                  // create a new grid
                  byte[][] temp_grid = new byte[height][];
                  int lower_width = value;
                  if (lower_width > width)
                  {
                     lower_width = width;
                  }
                  // copy values over
                  for (int i = 0; i < height; i++)
                  {
                     temp_grid[i] = new byte[value];
                     for (int j = 0; j < lower_width; j++)
                     {
                        temp_grid[i][j] = grid[i][j];
                     }
                  }

                  // reassign grid
                  grid = temp_grid;
               }
            }
            else
            {
               throw new ArgumentException("ByteGrid: Invalid width parameter. Must be greater than 0.");
            }
         }
      }
      public bool valid_grid
      {
         get { return height > 0 && width > 0; }
      }
      public byte[][] grid;
   }
}
