namespace Domain.Identity.ValueObjects
{
    public sealed class HashedPassword : IEquatable<HashedPassword>
    {
        public string Value { get; }

        private HashedPassword(string hash)
        {
            Value = hash;
        }

        public static HashedPassword From(string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                throw new ArgumentException("Hashed password cannot be null or empty.", nameof(hashedPassword));
            }

            return new HashedPassword(hashedPassword);
        }

        public bool Equals(HashedPassword? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }

        public override bool Equals(object? obj) => Equals(obj as HashedPassword);

        public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => Value;

        public static bool operator ==(HashedPassword? left, HashedPassword? right) => Equals(left, right);

        public static bool operator !=(HashedPassword? left, HashedPassword? right) => !Equals(left, right);
    }
}
