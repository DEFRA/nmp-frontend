using NMP.Commons.Models;
using NMP.Commons.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Commons.ViewModels;

public class TermsOfUseViewModel: TermsOfUse
{
    public new bool IsTermsOfUseAccepted { get; set; }
}
