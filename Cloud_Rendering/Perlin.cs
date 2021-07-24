﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Cloud_Rendering
{
    public class Perlin
    {
        public int repeat;
        public int[] p;
        public int[] p2;
        public Vector2[] vecs;
        public bool disposed;
        float[,] pointvalue;
        float pointvalue00;
        float pointvalue10;
        float pointvalue01;
        float pointvalue11;
        public Perlin(int repeat, int Seed)
        {
            this.repeat = repeat;
            Random r5 = new Random();
            permutation = new int[256];
            p2 = new int[1024];
            vecs = new Vector2[1024];
            pointvalue = new float[2, 2];
            for (int i = 0; i < 256; i++)
            {
                permutation[i] = r5.Next(0, 256);
            }
            for (int i = 0; i < 1024; i++)
            {
                p2[i] = r5.Next(0, 1024);
            }
            for (int i = 0; i < 1024; i++)
            {
                float rotation = (float)((r5.Next(0, 10000) / 10000.0f) * Math.PI);
                vecs[i] = new Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation)) * (r5.Next(1, 1000) / 1000.0f);
            }
            Perlin2();
        }
        public double OctavePerlin(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;            // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                total += perlin(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }
            return total / maxValue;
        }

        public int[] permutation;
        public int[] permutation2;/* = { 151,160,137,91,90,15,					// Hash lookup table as defined by Ken Perlin.  This is a randomly
		131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,	// arranged array of all numbers from 0-255 inclusive.
		190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180};*/                                                // Doubled permutation to avoid overflow
        void Perlin2()
        {
            p = new int[255];
            Random r = new Random();
            for (int x = 0; x < 255; x++)
            {
                p[x] = r.Next(0, 256);
            }
        }

        public double speedOctavePerlin2D(float x, float y, float z, int octaves, double persistence)
        {
            double total = 0;
            float frequency = 1;
            double amplitude = 1;
            double maxValue = 0;            // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                total += speedperlin3D(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }
            return total / maxValue;
        }

        public float speedperlin3D(float x, float y, float z)
        {
            uint xcoo, ycoo, zcoo;
            xcoo = (uint)x;
            ycoo = (uint)y;
            zcoo = (uint)z;
            float pointvalue000 = p[(int)((p[(((xcoo) * 734) % 255)] * 0.3333f + p[(((ycoo) * 346) % 255)] * 0.3333f + p[(((zcoo) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float pointvalue010 = p[(int)((p[(((xcoo) * 734) % 255)] * 0.3333f + p[(((ycoo + 1) * 346) % 255)] * 0.3333f + p[(((zcoo) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float pointvalue100 = p[(int)((p[(((xcoo + 1) * 734) % 255)] * 0.3333f + p[(((ycoo) * 346) % 255)] * 0.3333f + p[(((zcoo) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float pointvalue110 = p[(int)((p[(((xcoo + 1) * 734) % 255)] * 0.3333f + p[(((ycoo + 1) * 346) % 255)] * 0.3333f + p[(((zcoo) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float pointvalue001 = p[(int)((p[(((xcoo) * 734) % 255)] * 0.3333f + p[(((ycoo) * 346) % 255)] * 0.3333f + p[(((zcoo + 1) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float pointvalue011 = p[(int)((p[(((xcoo) * 734) % 255)] * 0.3333f + p[(((ycoo + 1) * 346) % 255)] * 0.3333f + p[(((zcoo + 1) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float pointvalue101 = p[(int)((p[(((xcoo + 1) * 734) % 255)] * 0.3333f + p[(((ycoo) * 346) % 255)] * 0.3333f + p[(((zcoo + 1) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float pointvalue111 = p[(int)((p[(((xcoo + 1) * 734) % 255)] * 0.3333f + p[(((ycoo + 1) * 346) % 255)] * 0.3333f + p[(((zcoo + 1) * 452) % 255)] * 0.3333f))] * 0.00390625f;
            float hohe = 0;
            hohe += fade(1 - (x - xcoo)) * pointvalue000 * fade(1 - (y - ycoo)) * fade(1 - (z - zcoo));
            hohe += fade(x - xcoo) * pointvalue100 * fade(1 - (y - ycoo)) * fade(1 - (z - zcoo));
            hohe += fade(1 - (x - xcoo)) * pointvalue010 * fade(y - ycoo) * fade(1 - (z - zcoo));
            hohe += fade(x - xcoo) * pointvalue110 * fade(y - ycoo) * fade(1 - (z - zcoo));

            hohe += fade(1 - (x - xcoo)) * pointvalue001 * fade(1 - (y - ycoo)) * fade(z - zcoo);
            hohe += fade(x - xcoo) * pointvalue101 * fade(1 - (y - ycoo)) * fade(z - zcoo);
            hohe += fade(1 - (x - xcoo)) * pointvalue011 * fade(y - ycoo) * fade(z - zcoo);
            hohe += fade(x - xcoo) * pointvalue111 * fade(y - ycoo) * fade(z - zcoo);
            return (hohe - 0.5f) * 2.0f;
        }

        public double perlin(double x, double y, double z)
        {
            if (repeat > 0)
            {                                   // If we have any repeat on, change the coordinates to their "local" repetitions
                x = x % repeat;
                y = y % repeat;
                z = z % repeat;
            }
            int xi = (int)x & 255;                              // Calculate the "unit cube" that the point asked will be located in
            int yi = (int)y & 255;                              // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            int zi = (int)z & 255;                              // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            double xf = x - (int)x;                             // We also fade the location to smooth the result.
            double yf = y - (int)y;
            double zf = z - (int)z;
            double u = fade((float)xf);
            double v = fade((float)yf);
            double w = fade((float)zf);
            int aaa, aba, aab, abb, baa, bba, bab, bbb;
            aaa = p[p[p[xi] + yi] + zi];
            aba = p[p[p[xi] + inc(yi)] + zi];
            aab = p[p[p[xi] + yi] + inc(zi)];
            abb = p[p[p[xi] + inc(yi)] + inc(zi)];
            baa = p[p[p[inc(xi)] + yi] + zi];
            bba = p[p[p[inc(xi)] + inc(yi)] + zi];
            bab = p[p[p[inc(xi)] + yi] + inc(zi)];
            bbb = p[p[p[inc(xi)] + inc(yi)] + inc(zi)];
            double x1, x2, y1, y2;
            x1 = lerp(grad(aaa, xf, yf, zf),                // The gradient function calculates the dot product between a pseudorandom
                        grad(baa, xf - 1, yf, zf),              // gradient vector and the vector from the input coordinate to the 8
                        u);                                     // surrounding points in its unit cube.
            x2 = lerp(grad(aba, xf, yf - 1, zf),                // This is all then lerped together as a sort of weighted average based on the faded (u,v,w)
                        grad(bba, xf - 1, yf - 1, zf),              // values we made earlier.
                          u);
            y1 = lerp(x1, x2, v);
            x1 = lerp(grad(aab, xf, yf, zf - 1),
                        grad(bab, xf - 1, yf, zf - 1),
                        u);
            x2 = lerp(grad(abb, xf, yf - 1, zf - 1),
                          grad(bbb, xf - 1, yf - 1, zf - 1),
                          u);
            y2 = lerp(x1, x2, v);
            return (lerp(y1, y2, w) + 1) / 2;                       // For convenience we bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        }
        public int inc(int num)
        {
            num++;
            if (repeat > 0) num %= repeat;
            return num;
        }
        public static double grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;                                  // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
            double u = h < 8 /* 0b1000 */ ? x : y;              // If the most significant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.
            double v;                                           // In Ken Perlin's original implementation this was another conditional operator (?:).  I
            // expanded it for readability.
            if (h < 4 /* 0b0100 */)                             // If the first and second significant bits are 0 set v = y
                v = y;
            else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)// If the first and second significant bits are 1 set v = x
                v = x;
            else                                                // If the first and second significant bits are not equal (0/1, 1/0) set v = z
                v = z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v); // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
        }
        public static float fade(float t)
        {
            // Fade function as defined by Ken Perlin.  This eases coordinate values
            // so that they will "ease" towards integral values.  This ends up smoothing
            // the final output.
            return (t * t * t * (t * (t * 6 - 15) + 10));         // 6t^5 - 15t^4 + 10t^3
        }
        public static double lerp(double a, double b, double x)
        {
            return a + x * (b - a);
        }
    }
}