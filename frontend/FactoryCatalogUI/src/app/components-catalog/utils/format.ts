import { Factory } from "../../../entities/factory";

const NO_VALUE = "-";

export const formatQuantity = (value: number | null | undefined): string =>
    value === null || value === undefined ? NO_VALUE : value.toLocaleString(undefined, { maximumFractionDigits: 4 });

export const getFactoryCode = (factories: Factory[], factoryId: string): string =>
    factories.find(factory => factory.id === factoryId)?.countryCode || factoryId;

export const parseLevel = (text: string): number | null | undefined => {
    if (text.trim() === "") return null;
    const level = Number(text);
    return Number.isFinite(level) && level >= 0 ? level : undefined;
};
