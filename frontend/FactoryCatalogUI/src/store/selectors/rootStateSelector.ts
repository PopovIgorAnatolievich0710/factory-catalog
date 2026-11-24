import {TypedUseSelectorHook, useSelector} from "react-redux";
import {RootState} from "../rootReducer";

export const useRootStateSelector: TypedUseSelectorHook<RootState> = useSelector;
