namespace Shor
{
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Primitive;
	open Microsoft.Quantum.Extensions.Math;

    operation U_xN(qubits : Qubit[], constant : Int, N : Int) : () {
        body {
            ModularMultiplyByConstantLE(constant, N, LittleEndian(qubits));
        }
        controlled auto;
    }

    function quickPower(x : Int, y : Int) : (Int) {
        mutable tmp = 1;
        mutable ans = 1;
        mutable yy = y;
        for (i in 0 .. 20) {
            if (yy % 2 == 1) {
                set ans = ans * tmp;
            }
            set yy = yy / 2;
            if (yy == 0) {
                return ans;
            }
            set tmp = tmp * x;
        }
        return ans;
    }

    function quickPowerWithModule(x : Int, y : Int, N : Int) : (Int) {
        mutable tmp = 1;
        mutable ans = 1;
        mutable yy = y;
        for (i in 0 .. 20) {
            if (yy % 2 == 1) {
                set ans = (ans * tmp) % N;
            }
            set yy = yy / 2;
            if (yy == 0) {
                return ans;
            }
            set tmp = (tmp * x) % N;
        }
        return ans;
    }


    operation measure(qubits : Qubit[], t : Int) : (Int[])
    {
        body {
            mutable measurement = new Int[t];
            for (i in 0 .. t - 1) {
                if (M(qubits[i]) == One) {
                    set measurement[i] = 1;
                } else {
                    set measurement[i] = 0;
                }
            }
            return measurement;
        }
    }

    operation quantumFourierTransform(qubits : Qubit[]) : () {
        body {
			let pi = PI();
            let n = Length(qubits);
            mutable powers = new Double[n + 5];
            set powers[0] = 1.0;
            for (i in 1 .. n) {
                set powers[i] = powers[i - 1] * 2.0;
            }

            for (i in 0 .. n - 1) {
                H(qubits[i]);
                for (j in i + 1 .. n - 1) {
                    (Controlled R1) ([qubits[j]], (2.0 * pi / powers[j - i + 1], qubits[i]));
                }
            }
            for (i in 0 .. n / 2 - 1) {
                SWAP(qubits[i], qubits[n - 1 - i]);
            }
        }
        adjoint {
			let pi = PI();
            let n = Length(qubits);
            mutable powers = new Double[n + 5];
            set powers[0] = 1.0;
            for (i in 1 .. n) {
                set powers[i] = powers[i - 1] * 2.0;
            }

            for (i in 0 .. n / 2 - 1) {
                SWAP(qubits[i], qubits[n - 1 - i]);
            }
            for (p in 0 .. n - 1) {
                let i = n - 1 - p;
                for (q in 0 .. p - 1) {
                    let j = n - 1 - q;
                    (Controlled R1) ([qubits[j]], (-2.0 * pi / powers[j - i + 1], qubits[i]));
                }
                H(qubits[i]);
            }
        }
    }

    operation quantumOrderFinding(a : Int, N : Int) : (Int [])
    {
        body {
            let l = 5;
            let t = 7;
            mutable result = new Int[t];
            using (qs = Qubit[t + l]) {
                let x = qs[0 .. t - 1];
                let y = qs[t .. t + l - 1];
                for (i in 0 .. t - 1) {
                    H(x[i]);
                }
                X(y[0]);
                for (i in 0 .. t - 1) {
                    let r = t - 1 - i;
                    (Controlled U_xN) ([x[i]], (y, quickPowerWithModule(a, quickPower(2, r), N), N));
                }
                (Adjoint quantumFourierTransform) (x);
                set result = measure(x, t);
                ResetAll(qs);
            }
			return result;
        }
    }
}
