using FluentValidation;
using SolutionsTools.WebApi.Models;

namespace SolutionsTools.WebApi
{
    public class ClientValidator : AbstractValidator<Client>
    {
        public ClientValidator()
        {
            RuleSet("AddRule", () => {
                RuleFor(c => c.Nome).NotEmpty();
                RuleFor(c => c.Sobrenome).NotEmpty();
                RuleFor(c => c.Cpf).NotEmpty();
                RuleFor(c => c.Email).NotEmpty();
                RuleFor(c => c.Departamento).NotEmpty();
                RuleFor(c => c.Cargo).NotEmpty();
                RuleFor(c => c.Cnpj).NotEmpty();
            });

            RuleSet("UpdRule", () => {
                RuleFor(c => c.Cpf).NotEmpty();
                RuleFor(c => c.Departamento).NotEmpty();
                RuleFor(c => c.Cargo).NotEmpty();
            });

            RuleSet("DelRule", () => {
                RuleFor(c => c.Cpf).NotEmpty();
            });

            //RuleFor(vistoria => vistoria.dataRealizacao).GreaterThan(DateTime.Today.AddYears(-5));
            //RuleFor(vistoria => vistoria.partesAvariadas).NotEmpty();
            //RuleForEach(p => p.partesAvariadas).SetValidator(new ParteAvariadaValidator());
        }  
    }
}