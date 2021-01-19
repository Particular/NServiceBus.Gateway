namespace NServiceBus.Gateway.Channels
{
    using System;

    /// <summary>
    /// The site channel class.
    /// </summary>
    public class Channel : IEquatable<Channel>
    {
        /// <summary>
        /// The type of the channel.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The address to receive/send on.
        /// </summary>
        public string Address { get; set; }

        internal static Channel Parse(string s)
        {
            var parts = s.Split(',');

            return new Channel
            {
                Type = parts[0],
                Address = parts[1]
            };
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Channel other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.Type, Type) && Equals(other.Address, Address);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return $"{Type},{Address}";
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof(Channel))
            {
                return false;
            }
            return Equals((Channel)obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type?.GetHashCode() ?? 0) * 397) ^ (Address?.GetHashCode() ?? 0);
            }
        }

        /// <summary>
        /// Overrides the == operator.
        /// </summary>
        public static bool operator ==(Channel left, Channel right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Overrides the != operator.
        /// </summary>
        public static bool operator !=(Channel left, Channel right)
        {
            return !Equals(left, right);
        }
    }
}