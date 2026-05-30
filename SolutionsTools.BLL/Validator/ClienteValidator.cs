using FluentValidation;
using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionsTools.BLL.Validator
{
    public class ClienteValidator : AbstractValidator<Cliente>
    {
        public ClienteValidator()
        {
            RuleFor(cliente => cliente.Nome).NotEmpty();
            RuleFor(cliente => cliente.Sobrenome).NotEmpty();
            RuleFor(vistoria => vistoria.Cpf).NotEmpty();
            RuleFor(vistoria => vistoria.Departamento).NotEmpty();
            RuleFor(vistoria => vistoria.Cargo).NotEmpty();
            //RuleFor(vistoria => vistoria.).NotEmpty();
            //RuleFor(vistoria => vistoria.dataRealizacao).GreaterThan(DateTime.Today.AddYears(-5));
            //RuleFor(vistoria => vistoria.dataSolicitacao).GreaterThan(DateTime.Today.AddYears(-5));
            //RuleFor(vistoria => vistoria.partesAvariadas).NotEmpty();
            //RuleForEach(p => p.partesAvariadas).SetValidator(new ParteAvariadaValidator());

        }

    }
}
