import { CONSTANTS } from "../core/CONSTANTS";
import { ColumnFilter, FilterOperator, FilterOperators, ListFilters, ListRequest, SortDirection } from "../entities/list";
import { isFilterActive } from "./table-filter";

const FILTER_PREFIX = "filter.";
const VALUE_TO_SUFFIX = "to";
const LIST_KEYS = ["page", "pageSize", "search", "sortBy", "sortDirection"];
const operators: string[] = Object.values(FilterOperators);

export interface ListQueryDefaults {
    sortBy: string | null;
    sortDirection: SortDirection;
}

const toPositiveInt = (value: string | null, fallback: number): number => {
    const parsed = Number.parseInt(value ?? "", 10);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
};

const toSortDirection = (value: string | null, fallback: SortDirection): SortDirection =>
    value === "asc" || value === "desc" ? value : fallback;

const splitFilterKey = (key: string): { field: string; suffix: string | null } => {
    const name = key.substring(FILTER_PREFIX.length);
    const lastDot = name.lastIndexOf(".");
    const suffix = lastDot > 0 ? name.substring(lastDot + 1) : null;

    if (suffix !== null && (operators.includes(suffix) || suffix === VALUE_TO_SUFFIX)) {
        return { field: name.substring(0, lastDot), suffix };
    }
    return { field: name, suffix: null };
};

export const parseListQuery = (queryString: string, defaults: ListQueryDefaults): ListRequest => {
    const params = new URLSearchParams(queryString);
    const filters: ListFilters = {};

    params.forEach((value, key) => {
        if (!key.startsWith(FILTER_PREFIX)) return;

        const { field, suffix } = splitFilterKey(key);
        const filter: ColumnFilter = filters[field] ?? {};

        if (suffix === null) {
            filter.values = [...(filter.values ?? []), value];
        } else if (suffix === VALUE_TO_SUFFIX) {
            filter.valueTo = value;
        } else {
            filter.operator = suffix as FilterOperator;
            filter.value = value;
        }
        filters[field] = filter;
    });

    return {
        page: toPositiveInt(params.get("page"), CONSTANTS.PAGING.DEFAULT_PAGE),
        pageSize: toPositiveInt(params.get("pageSize"), CONSTANTS.PAGING.DEFAULT_PAGE_SIZE),
        search: params.get("search") ?? "",
        sortBy: params.get("sortBy") ?? defaults.sortBy,
        sortDirection: toSortDirection(params.get("sortDirection"), defaults.sortDirection),
        filters,
    };
};

export const buildListQuery = (
    request: ListRequest,
    defaults: ListQueryDefaults,
    currentQueryString: string,
): string => {
    const params = new URLSearchParams(currentQueryString);
    Array.from(params.keys())
        .filter(key => key.startsWith(FILTER_PREFIX) || LIST_KEYS.includes(key))
        .forEach(key => params.delete(key));

    if (request.search) params.set("search", request.search);
    if (request.sortBy && (request.sortBy !== defaults.sortBy || request.sortDirection !== defaults.sortDirection)) {
        params.set("sortBy", request.sortBy);
        params.set("sortDirection", request.sortDirection);
    }
    if (request.page !== CONSTANTS.PAGING.DEFAULT_PAGE) params.set("page", String(request.page));
    if (request.pageSize !== CONSTANTS.PAGING.DEFAULT_PAGE_SIZE) params.set("pageSize", String(request.pageSize));

    Object.entries(request.filters)
        .filter(([, filter]) => isFilterActive(filter))
        .forEach(([field, filter]) => {
            if (filter.values && filter.values.length > 0) {
                filter.values.forEach(value => params.append(`${FILTER_PREFIX}${field}`, value));
                return;
            }
            params.set(`${FILTER_PREFIX}${field}.${filter.operator}`, filter.value);
            if (filter.operator === FilterOperators.Between && filter.valueTo) {
                params.set(`${FILTER_PREFIX}${field}.${VALUE_TO_SUFFIX}`, filter.valueTo);
            }
        });

    return params.toString();
};
