using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.ViewModels;

public class TermsOfUseViewModel
{
    [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblIagree_to_the_terms_of_use))]
    public bool IsAccepted { get; set; }
}
