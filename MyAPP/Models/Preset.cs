namespace MyAPP.Models
{
    public sealed class Preset
    {
        public System.Guid Id { get; set; }

        public System.Guid UserId { get; set; }

        public System.String Name { get; set; } = string.Empty;

        public System.DateTime CreatedAt { get; set; }

        public System.Collections.Generic.List<Template> Templates { get; set; } = new System.Collections.Generic.List<Template>();
    }
}