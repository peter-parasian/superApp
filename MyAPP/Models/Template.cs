namespace MyAPP.Models
{
    public sealed class Template
    {
        public System.Guid Id { get; set; }

        public System.Guid PresetId { get; set; }

        public System.String Title { get; set; } = string.Empty;

        public System.String Content { get; set; } = string.Empty;

        public System.DateTime CreatedAt { get; set; }

        public System.Collections.Generic.List<Variable> Variables { get; set; } = new System.Collections.Generic.List<Variable>();
    }
}