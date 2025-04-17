﻿using System;

namespace ExtendedVariants.Variants {
    public class BadelineBossCount : AbstractExtendedVariant {
        public BadelineBossCount() : base(variantType: typeof(int), defaultVariantValue: 1) { }

        public override object ConvertLegacyVariantValue(int value) {
            return value;
        }
    }
}
