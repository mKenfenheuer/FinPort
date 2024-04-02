using System.ComponentModel.DataAnnotations;

namespace FinPort.Models;

public class SettingViewModel
{
    [Display(Name = "Push Portfolio Details to Home Assistant")]
    public bool PushPortfolioDetailsToHomeAssistant { get; set; }
    [Display(Name = "Push Portfolio Position Details to Home Assistant")]
    public bool PushPositionDetailsToHomeAssistant { get; set; }
}