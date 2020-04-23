using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

// Origins from https://remibou.github.io/Using-the-Blazor-form-validation/
namespace BlazorServerIdentityInterop.Areas.Identity.Components
{
    public class ServerSideValidator : ComponentBase
    {
        private ValidationMessageStore _messageStore;

        [CascadingParameter] EditContext CurrentEditContext { get; set; }

        /// <inheritdoc />  
        protected override Task OnParametersSetAsync()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(ServerSideValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(ServerSideValidator)} " +
                    $"inside an {nameof(EditForm)}.");
            }

            _messageStore = new ValidationMessageStore(CurrentEditContext);
            CurrentEditContext.OnValidationRequested += (s, e) => _messageStore.Clear();
            CurrentEditContext.OnFieldChanged += (s, e) => _messageStore.Clear(e.FieldIdentifier);
            return base.OnParametersSetAsync();
        }

        public void DisplayErrors(Dictionary<string, List<string>> errors)
        {
            foreach (var err in errors)
            {
                _messageStore.Add(CurrentEditContext.Field(err.Key), err.Value);
            }
            CurrentEditContext.NotifyValidationStateChanged();
        }
        public void DisplayErrors()
        {
            CurrentEditContext.NotifyValidationStateChanged();
        }

        public void AddError(FieldIdentifier field, string errMessage)
        {
            _messageStore.Add(field, errMessage);
        }

        public void AddError(object model, string fieldName, string errMessage)
        {
            var field = new FieldIdentifier(model, fieldName);
            _messageStore.Add(field, errMessage);
            CurrentEditContext.NotifyValidationStateChanged();
        }
    }
}

