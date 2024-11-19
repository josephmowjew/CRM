using UCS_CRM.Core.Models;
using UCS_CRM.Models;

namespace UCS_CRM.ViewModel;

public class SystemConfigurationViewModel
{
    public SystemDateConfiguration DateConfiguration { get; set; }
    public List<Holiday> Holidays { get; set; }
}