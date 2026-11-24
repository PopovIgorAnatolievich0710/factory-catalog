import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { loadFactories } from "../../app/factories/store/actions";
import { useRootStateSelector } from "./rootStateSelector";

export const useFactories = () => {
    const dispatch = useDispatch();
    const { items, status } = useRootStateSelector(state => state.factories);

    useEffect(() => {
        dispatch(loadFactories() as any);
    }, [dispatch]);

    return {
        factories: items,
        isLoading: status === "idle" || status === "loading",
    };
};
