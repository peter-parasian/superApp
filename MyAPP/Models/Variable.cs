namespace MyAPP.Models
{
    public sealed class Variable
    {
        public System.Guid Id { get; set; }

        public System.Guid TemplateId { get; set; }

        public System.String Name { get; set; } = string.Empty;

        public System.String? Value { get; set; }

        public System.DateTime CreatedAt { get; set; }
    }
}