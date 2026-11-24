import React from "react";
import { Api } from "../../helpers/api-helper";
import { ListRequest, TableSort } from "../../entities/list";
import { DataTableColumn } from "../data-table";

export interface CatalogColumn<T> extends DataTableColumn<T> {
    alwaysVisible?: boolean;
}

export interface CatalogListContext {
    request: ListRequest;
    visibleColumnKeys: string[];
    totalCount: number;
    reload: () => void;
}

export interface CatalogListSelectionContext extends CatalogListContext {
    clearSelection: () => void;
}

export interface CatalogListPageProps<T> {
    listKey: string;
    title: string;
    subtitle?: string;
    resource: string;
    api?: Api;
    columns: CatalogColumn<T>[];
    getRowId: (row: T) => string;
    getRowUrl?: (row: T) => string;
    defaultSort?: TableSort;
    searchPlaceholder?: string;
    headerInfo?: React.ReactNode;
    headerActions?: React.ReactNode;
    summary?: React.ReactNode;
    toolbarActions?: (context: CatalogListContext) => React.ReactNode;
    isActionRequired?: (row: T) => boolean;
    renderRowTags?: (row: T) => React.ReactNode;
    selectable?: boolean;
    isRowSelectable?: (row: T) => boolean;
    bulkActions?: (selectedIds: string[], context: CatalogListSelectionContext) => React.ReactNode;
}
