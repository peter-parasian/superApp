using MahApps.Metro.IconPacks;
using System.Reflection;
using TextCopy;
using Coll = System.Collections.Generic;
using Comp = System.ComponentModel;
using Controls = System.Windows.Controls;
using Dialogs = Microsoft.Win32;
using IO = System.IO;
using Linq = System.Linq;
using Media = System.Windows.Media;
using Models = MyAPP.Models;
using Msg = System.Windows.MessageBox;
using Obj = System.Collections.ObjectModel;
using Services = MyAPP.Services;
using Sys = System;
using Tasks = System.Threading.Tasks;
using Threading = System.Threading;
using Win = System.Windows;

namespace MyAPP.Views
{
    public partial class DashboardView
    {
        #region Toast Notification Helper

        private async void ShowToast(Sys.String message, Sys.Boolean isError = false, Sys.Boolean isDelete = false)
        {
            this.ToastText.Text = message;

            if (isError)
            {
                this.ToastIcon.Kind = PackIconMaterialKind.AlertCircle;
                this.ToastIcon.Foreground = Media.Brushes.Red;
            }
            else if (isDelete)
            {
                this.ToastIcon.Kind = PackIconMaterialKind.CheckCircle;
                this.ToastIcon.Foreground = Media.Brushes.Orange;
            }
            else
            {
                this.ToastIcon.Kind = PackIconMaterialKind.CheckCircle;
                this.ToastIcon.Foreground = Media.Brushes.LightGreen;
            }

            this.ToastNotification.Visibility = Win.Visibility.Visible;
            this.ToastNotification.Opacity = 1;

            await Tasks.Task.Delay(1500);

            this.ToastNotification.Visibility = Win.Visibility.Collapsed;
        }

        #endregion
    }
}