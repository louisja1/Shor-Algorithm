using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using System;
using System.IO;
using System.Text;

namespace Shor
{
    class Driver
    {
        static long gcd(long a, long b)
        {
            if (b == 0)
            {
                return a;
            }
            return gcd(b, a % b);
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Please input the number to be factored...");
            long N = Convert.ToInt64(Console.ReadLine());
            
            long p = factor(N);
            Console.WriteLine("Success!");
            Console.WriteLine("-·-·-·-·-·-·-·-·-·-·-·-·-·-·-·-");
            Console.WriteLine($"{N} = {p} * {N / p}");
            Console.ReadKey();
        }

        static long[] continuedFractionExtension(double x)
        {
            double PRECISION = 1e-9;
            int ITERATION = 100;
            long[] fractions;
            fractions = new long[100];
            long integer = 0;
            double frac = 0;
            double currentNum = x;
            int i = 0;
            for(int ii = 0; ii < 100; ++ii)
            {
                fractions[ii] = -1;
            }
            while (true)
            {
                integer = (long)currentNum;
                fractions[i++] = integer;
                frac = currentNum - integer;
                if (frac < PRECISION || i >= ITERATION)
                {
                    break;
                }
                currentNum = 1 / frac;
            }
            return fractions;
        }

        static Frac[] restoreContinuedFraction(long[] arr)
        {
            int i = 0;
            Frac[] fracArray = new Frac[100];
            while (true)
            {
                if (arr[i] == -1)
                {
                    fracArray[i++] = new Frac(-1, 1);
                    if(i == 100)
                    {
                        break;
                    }
                    continue;
                }
                Frac cur = new Frac(0, 1);
                for (int j = i; j >= 0; --j)
                {
                    cur = cur.add(new Frac(arr[j], 1));
                    if (j == 0)
                    {
                        fracArray[i++] = cur;
                    }
                    else
                    {
                        cur = cur.inverse();
                    }
                }
            }
            return fracArray;
        }

        static long factor(long N)
        {
            while (true)
            {
                if (N % 2 == 0)
                {
                    return 2;
                }
                Random ran = new Random();
                int x = ran.Next(1, (int)(N - 1));
                Console.WriteLine($"generate random number: {x}");
                long g = gcd(x, N);
                if (g != 1 && g != N)
                {
                    return g;
                }
                long r = qQrderFinding(x, N);
                if (r == 0 || r == -1)
                {
                    Console.WriteLine("Fail...");
                    continue;
                }
                if (r % 2 == 0 && modCondition(x, r, N))
                {
                    long g1 = gcd(quickPower(x, r / 2, N) - 1, N);
                    if (g1 != 1 && g1 != N)
                    {
                        return g1;
                    }
                    long g2 = gcd(quickPower(x, r / 2, N) + 1, N);
                    if (g2 != 1 && g2 != N)
                    {
                        return g2;
                    }
                }
                Console.WriteLine("Fail...");
            }
        }

        static bool modCondition(long x, long r, long N)
        {
            return (quickPower(x, r / 2, N) + N) % N != N - 1;
        }

        static long quickPower(long x, long y, long N)
        {
            if (y == 0)
            {
                return 1;
            }
            long tmp = quickPower(x, y / 2, N);
            long ans = (tmp * tmp) % N;
            if (y % 2 == 1)
            {
                ans = (ans * x) % N;
            }
            return ans;
        }

        static long qQrderFinding(long x, long N)
        {
            double sr = getPhaseEstimation(x, N);
            if(sr == 0)
            {
                return -1;
            }
            //Console.WriteLine($"sr: {sr}");
            long[] arr = continuedFractionExtension(sr);
            Frac[] fracArray = restoreContinuedFraction(arr);
            foreach (Frac item in fracArray)
            {
                long r = item.dominator;
                if (quickPower(x, r, N) % N == 1)
                {
                    return r;
                }
            }
            return 0;
        }

        static double getPhaseEstimation(long a, long N)
        {
            // Console.WriteLine($"getPhaseEstimation: the random number：{a}, N: {N}");
            double sr = 0;
            double dom = 1.0;
            int TBIT = 7;
            using (var sim = new QuantumSimulator())
            {
                var res = quantumOrderFinding.Run(sim, a, N).Result;
                for (int i = 0; i < TBIT; i++)
                {
                    dom *= 2.0;
                    if (res[i] == 1)
                    {
                        sr = sr * 2 + 1;
                    }
                    else
                    {
                        sr *= 2;
                    }
                }
                sr /= dom;
            }
            return sr;
        }

        class Frac
        {
            public long numerator, dominator;
            public Frac()
            {
                numerator = 0;
                dominator = 1;
            }
            public Frac(long _numerator, long _dominator)
            {
                numerator = _numerator;
                dominator = _dominator;
            }

            public Frac add(Frac other)
            {
                long newDom = this.dominator * other.dominator;
                long newNum = this.numerator * other.dominator + other.numerator * this.dominator;
                Frac ret = new Frac(newNum, newDom);
                return ret.reduce();
            }
            public Frac reduce()
            {
                long d = gcd(numerator, dominator);
                numerator /= d;
                dominator /= d;
                return this;
            }
            public long gcd(long a, long b)
            {
                if (b == 0)
                {
                    return a;
                }
                return gcd(b, a % b);
            }

            public void print()
            {
                Console.WriteLine("{0}/{1}", numerator, dominator);
            }

            public Frac inverse()
            {
                return new Frac(dominator, numerator);
            }
        }
    }
}
