// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;

namespace Squidex.Assets
{
    public sealed class ResizeOptions
    {
        public ImageFormat? Format { get; set; }

        public ResizeMode Mode { get; set; }

        public int? TargetWidth { get; set; }

        public int? TargetHeight { get; set; }

        public int? Quality { get; set; }

        public float? FocusX { get; set; }

        public float? FocusY { get; set; }

        public string? Background { get; set; }

        public bool IsValid
        {
            get { return TargetWidth > 0 || TargetHeight > 0 || Quality > 0 || Format != null; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(TargetWidth);
            sb.Append("_");
            sb.Append(TargetHeight);
            sb.Append("_");
            sb.Append(Mode);

            if (Quality.HasValue)
            {
                sb.Append("_");
                sb.Append(Quality);
            }

            if (FocusX.HasValue)
            {
                sb.Append("_focusX_");
                sb.Append(FocusX);
            }

            if (FocusY.HasValue)
            {
                sb.Append("_focusY_");
                sb.Append(FocusY);
            }

            if (Format != null)
            {
                sb.Append("_format_");
                sb.Append(Format.ToString());
            }

            if (!string.IsNullOrWhiteSpace(Background))
            {
                sb.Append("_background_");
                sb.Append(Background);
            }

            return sb.ToString();
        }
    }
}
