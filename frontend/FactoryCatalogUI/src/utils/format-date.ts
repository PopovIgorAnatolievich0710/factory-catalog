import dayjs from "dayjs";

type DateInput = string | number | Date | null | undefined;

const NO_VALUE = "-";

const format = (value: DateInput, template: string): string => {
    if (value === null || value === undefined || value === "") return NO_VALUE;
    const date = dayjs(value);
    return date.isValid() ? date.format(template) : NO_VALUE;
};

export const formatDate = (value: DateInput): string => format(value, "DD/MM/YYYY");

export const formatDateTime = (value: DateInput): string => format(value, "DD/MM/YYYY HH:mm");
