using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BankApp.Web.Infrastructure;

/// <summary>
/// Türkçe kültürde "1.500" bin beş yüz demektir, ama tarayıcıların number input'u "1500.5" gönderir.
/// Bu binder iki formatı da güvenli şekilde decimal'e çevirir:
///   "1500.50" → 1500.50 | "1500,50" → 1500.50 | "1.500,50" → 1500.50
/// </summary>
public class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (string.IsNullOrWhiteSpace(value))
            return Task.CompletedTask; // Boş değer: [Required] doğrulaması yakalar

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value, value);

        var text = value.Trim().Replace(" ", string.Empty);
        if (text.Contains(',') && text.Contains('.'))
            text = text.Replace(".", string.Empty).Replace(',', '.');   // 1.500,50
        else if (text.Contains(','))
            text = text.Replace(',', '.');                             // 1500,50

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            bindingContext.Result = ModelBindingResult.Success(result);
        else
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Geçerli bir tutar giriniz.");

        return Task.CompletedTask;
    }
}

public class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = context.Metadata.UnderlyingOrModelType;
        return type == typeof(decimal) ? new DecimalModelBinder() : null;
    }
}
