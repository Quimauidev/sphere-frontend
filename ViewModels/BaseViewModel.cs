using CommunityToolkit.Mvvm.ComponentModel;
using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private UiViewState uiState = UiViewState.Loading;

        public bool IsLoading => UiState == UiViewState.Loading;
        public bool IsSuccess => UiState == UiViewState.Success;
        public bool IsEmpty => UiState == UiViewState.Empty;
        public bool IsError => UiState == UiViewState.Error;

        partial void OnUiStateChanged(UiViewState value)
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(IsSuccess));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsError));
        }

        [ObservableProperty]
        private string? errorMessage;
    }

}
