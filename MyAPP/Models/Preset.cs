namespace MyAPP.Models
{
    public sealed class Preset
    {
        public System.Guid Id { get; set; }

        public System.Guid UserId { get; set; }

        public System.String Name { get; set; } = string.Empty;

        public System.DateTime CreatedAt { get; set; }

        public System.Collections.Generic.List<Template> Templates { get; set; } = new System.Collections.Generic.List<Template>();

        // Modifikasi: Menambahkan properti VariableCount untuk menghitung total variabel dari semua templat
        public System.Int32 VariableCount
        {
            get
            {
                if (this.Templates == null)
                {
                    return 0;
                }

                // Menggunakan Fully Qualified Name untuk Linq Sum agar hemat memori (tidak perlu using tambahan)
                return System.Linq.Enumerable.Sum(this.Templates, t => t.Variables?.Count ?? 0);
            }
        }
    }
}