// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace RapidXamlToolkit
{
    public class AnalysisOptionsGrid : DialogPage
    {
        [DisplayName("Analyze On Save")]
        [Description("(Re)Analyze the XAML document every time it is saved.")]
        [DefaultValue(true)]
        public bool AnalyzeWhenDocumentSaved { get; set; } = true;

        [Category("Experimental")]
        [DisplayName("Enable Custom Analysis")]
        [Description("Attempt to load additional analyzers from referenced libraries.")]
        [DefaultValue(false)]
        public bool EnableCustomAnalysis { get; set; } = true;

        [Category("Experimental")]
        [DisplayName("Path To Custom Analyzers")]
        [Description("Attempt to load additional analyzers from referenced libraries.")]
        [DefaultValue("")]
        public string Path { get; set; } = string.Empty;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            RapidXamlAnalysisPackage.Options = this;
        }
    }
}
