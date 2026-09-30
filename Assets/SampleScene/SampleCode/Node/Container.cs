using System.Runtime.InteropServices;
using UnityEngine;

namespace SampleScene.SampleCode.Node
{
    public struct NumericValue
    {
        private enum NumericValueType
        {
            Int,
            Float,
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct NumericValueUnion
        {
            [FieldOffset(0)] public int AsInt;
            [FieldOffset(0)] public float AsFloat;
        }

        private NumericValueType _numericValueType;
        private NumericValueUnion _numericValueUnion;

        public static implicit operator int(NumericValue value)
        {
            switch (value._numericValueType)
            {
                case NumericValueType.Int:
                    return value._numericValueUnion.AsInt;
                case NumericValueType.Float:
                    return (int)value._numericValueUnion.AsFloat;
                default:
                    return 0;
            }
        }

        public static implicit operator NumericValue(int value)
        {
            var numericValue = new NumericValue
            {
                _numericValueType = NumericValueType.Int
            };
            numericValue._numericValueUnion.AsInt = value;
            return numericValue;
        }

        public static implicit operator float(NumericValue value)
        {
            switch (value._numericValueType)
            {
                case NumericValueType.Int:
                    return value._numericValueUnion.AsInt;
                case NumericValueType.Float:
                    return value._numericValueUnion.AsFloat;
                default:
                    return 0;
            }
        }

        public static implicit operator NumericValue(float value)
        {
            var numericValue = new NumericValue
            {
                _numericValueType = NumericValueType.Float
            };
            numericValue._numericValueUnion.AsFloat = value;
            return numericValue;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void RegisterMScriptCast()
        {
            MScript.MScript.SetCastValueToValue<int, NumericValue>(value => value);
            MScript.MScript.SetCastValueToValue<float, NumericValue>(value => value);
            MScript.MScript.SetCastValueToValue<NumericValue, int>(value => value);
            MScript.MScript.SetCastValueToValue<NumericValue, float>(value => value);
        }
    }
}