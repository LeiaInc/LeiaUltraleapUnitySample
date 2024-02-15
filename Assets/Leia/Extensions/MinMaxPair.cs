/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using UnityEngine;

namespace LeiaUnity
{
    /// <summary>
    /// Stores data for a min and max range. Guarantees that .max will always be higher value, and guarantees that .min will always be lower value. Assumes floats
    /// </summary>
    [System.Serializable]
    public class MinMaxPair: System.IEquatable<MinMaxPair>
    {
#pragma warning disable 414
        [SerializeField, HideInInspector] private float _min;
        [SerializeField, HideInInspector] private float _lifetimeMin = float.MinValue;
        [SerializeField, HideInInspector] private string _minLabel;
        [SerializeField, HideInInspector] private float _max;
        [SerializeField, HideInInspector] private float _lifetimeMax = float.MaxValue;
        [SerializeField, HideInInspector] private string _maxLabel;
#pragma warning restore 414

        private MinMaxPair() : this(0, 0)
        {
            // this constructor is used when a [SerializeField] MinMaxPair is declared in Unity.
            // this constructor intentionally left blank. chains up to ctor where user has provided current min/max
        }

        /// <summary>
        /// Initializes a MinMaxPair with data provided in currentMin and currentMax. Displays default "Min" and "Max" labels when inspected in Unity Editor
        /// </summary>
        /// <param name="currentMin">A number</param>
        /// <param name="currentMax">A number</param>
        public MinMaxPair(float currentMin, float currentMax): this(currentMin, "Min", currentMax, "Max")
        {
            // // this constructor intentionally left blank. chains up to ctor where user has provided labels "Min" and "Max" to be displayed in inspector
        }

        /// <summary>
        /// Initializes a MinMaxPair with data provided in currentMin and currentMax
        /// </summary>
        /// <param name="currentMin">A number</param>
        /// <param name="minLabel">A label to display alongside the min value in inspectors</param>
        /// <param name="currentMax">A number</param>
        /// <param name="maxLabel">A label to display alongside the max value in inspectors</param>
        public MinMaxPair(float currentMin, string minLabel, float currentMax, string maxLabel): this(currentMin, float.MinValue, minLabel, currentMax, float.MaxValue, maxLabel)
        {
            // this constructor intentionally left blank. chains up to ctor where user has provided lifetime min and max values that the current min and max are bounded by
        }

        /// <summary>
        /// Initializes a MinMaxPair with data provided in currentMin and currentMax
        /// </summary>
        /// <param name="currentMin">A number</param>
        /// <param name="lifetimeMin">A number which the current min shall never go below</param>
        /// <param name="minLabel">A label to display alongside the min value in inspectors</param>
        /// <param name="currentMax">A number</param>
        /// <param name="lifetimeMax">A number which the current max shall never go above</param>
        /// <param name="maxLabel">A label to display alongside the max value in inspectors</param>
        public MinMaxPair(float currentMin, float lifetimeMin, string minLabel, float currentMax, float lifetimeMax, string maxLabel)
        {
            // lifetime max should always be greater than lifetime min
            UnityEngine.Assertions.Assert.IsTrue(lifetimeMax >= lifetimeMin, "In MinMaxPair constructor you must provide a lifetimeMax which is greater than lifetimeMin");
            
            _lifetimeMin = lifetimeMin;
            _lifetimeMax = lifetimeMax;

            // but min and max can change order as user modifies the values. this allows user to not get locked up at min == max
            _min = Mathf.Max(lifetimeMin, Mathf.Min(currentMin, currentMax));
            _minLabel = minLabel;
            _max = Mathf.Min(lifetimeMax, Mathf.Max(currentMin, currentMax));
            _maxLabel = maxLabel;
        }

        /// <summary>
        /// Gets user-provided min
        /// </summary>
        public float min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = Mathf.Max(_lifetimeMin, value);
                _max = Mathf.Max(_min, _max);
            }
        }

        /// <summary>
        /// Gets user-provided max
        /// </summary>
        public float max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = Mathf.Min(_lifetimeMax, value);
                _min = Mathf.Min(_min, _max);
            }
        }

        /// <summary>
        /// Checks if a user-provided value is between min and max
        /// </summary>
        /// <param name="value">A value to check</param>
        /// <returns>True if value is in range</returns>
        public bool IsValueBetweenMinAndMax(float value)
        {
            return min <= value && value <= max;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}.\t{2}: {3}", _minLabel, _min, _maxLabel, _max);
        }

        public bool Equals(MinMaxPair other)
        {
            return Mathf.Approximately(other._min, this._min) && Mathf.Approximately(other._max, this._max);
        }
    }
}
