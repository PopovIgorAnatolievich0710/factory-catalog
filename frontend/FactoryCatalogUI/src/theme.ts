import { createTheme } from "@mui/material";
import { defaultThemeOptions } from "../bo-frontend/src/components/theme";

const factoryCatalogTheme = createTheme({
  ...defaultThemeOptions,
  components: {
    ...defaultThemeOptions.components,
    MuiTableCell: {
      ...defaultThemeOptions.components?.MuiTableCell,
      styleOverrides: {
        ...defaultThemeOptions.components?.MuiTableCell?.styleOverrides,
        stickyHeader: {
          ...(defaultThemeOptions.components?.MuiTableCell?.styleOverrides?.stickyHeader as object),
          fontWeight: 500,
        },
        head: {
          ...(defaultThemeOptions.components?.MuiTableCell?.styleOverrides?.head as object),
          fontWeight: 500,
        },
      },
    },
    MuiButton: {
      ...defaultThemeOptions.components?.MuiButton,
      styleOverrides: {
        ...defaultThemeOptions.components?.MuiButton?.styleOverrides,
        outlined: {
          ...(defaultThemeOptions.components?.MuiButton?.styleOverrides?.outlined as object),
          "&.Mui-disabled": {
            borderColor: "rgba(0, 0, 0, 0.12) !important",
          },
        },
      },
    },
  },
});

export default factoryCatalogTheme;
