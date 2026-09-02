import { createTheme, MantineColorsTuple, rem } from "@mantine/core";

/**
 * The app's one theme. Before this the MantineProvider ran on stock defaults (blue
 * primary, system font), so this file is where the product's look now lives: the
 * typeface, the accent, and a softer shadow ramp that the dashboard's borderless
 * widgets lean on to read as panels rather than outlined cards.
 */

// Indigo, replacing Mantine's default blue. Tailwind's indigo ramp: a balanced,
// contrast-checked scale, so per-board palette colours (still plain Mantine names)
// sit next to it without clashing. Index 6 is the filled/primary shade.
const brand: MantineColorsTuple = [
    "#eef2ff",
    "#e0e7ff",
    "#c7d2fe",
    "#a5b4fc",
    "#818cf8",
    "#6366f1",
    "#4f46e5",
    "#4338ca",
    "#3730a3",
    "#312e81",
];

const sansFontStack =
    '"Inter Variable", "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';

export const theme = createTheme({
    primaryColor: "brand",
    colors: { brand },

    fontFamily: sansFontStack,
    fontFamilyMonospace:
        'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, "Liberation Mono", monospace',

    headings: {
        fontFamily: sansFontStack,
        fontWeight: "600",
        sizes: {
            h1: { fontSize: rem(30), lineHeight: "1.2", fontWeight: "700" },
            h2: { fontSize: rem(23), lineHeight: "1.25", fontWeight: "650" },
            h3: { fontSize: rem(18), lineHeight: "1.3", fontWeight: "600" },
            h4: { fontSize: rem(16), lineHeight: "1.35", fontWeight: "600" },
            h5: { fontSize: rem(14), lineHeight: "1.4", fontWeight: "600" },
            h6: { fontSize: rem(12), lineHeight: "1.4", fontWeight: "600" },
        },
    },

    // Softer and more layered than Mantine's defaults.
    shadows: {
        xs: "0 1px 3px rgba(17, 20, 38, 0.08), 0 1px 2px rgba(17, 20, 38, 0.05)",
        sm: "0 2px 8px rgba(17, 20, 38, 0.08), 0 1px 3px rgba(17, 20, 38, 0.05)",
        md: "0 8px 24px rgba(17, 20, 38, 0.1), 0 3px 8px rgba(17, 20, 38, 0.07)",
        lg: "0 14px 36px rgba(17, 20, 38, 0.13), 0 5px 12px rgba(17, 20, 38, 0.08)",
        xl: "0 24px 52px rgba(17, 20, 38, 0.17), 0 9px 20px rgba(17, 20, 38, 0.1)",
    },
});
