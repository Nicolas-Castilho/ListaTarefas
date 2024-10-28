using System.ComponentModel.DataAnnotations;

namespace ListaTarefas.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "A tarefa deve ser nomeada")]
        [StringLength(20, ErrorMessage = "O nome da tarefa deve conter apenas 20 caractéres")]
        public string Nome { get; set; }
        [Required(ErrorMessage = "A tarefa precisa de uma descrição")]
        [StringLength(100, ErrorMessage = "A descrição deve conter no máximo 100 caractéres")]
        public string Descricao { get; set; }
        [Required(ErrorMessage = "Você precisa selecionar uma categoria")]
        [StringLength(16)]
        public string Categoria { get; set; }
        [Required(ErrorMessage = "Você precisa escolher um nível de prioridade")]
        [StringLength(5)]
        public string Prioridade { get; set; }
        public DateOnly DataVencimento { get; set; }
        public bool Completa {  get; set; }
        public string iduser { get; set; }

    }
}
