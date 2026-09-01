import { SaveFilterItemDto } from "../types/DashboardDto";
import { FilterForm } from "./FilterForm";

interface Props {
    color?: string;
    onBack: () => void;
    onAdd: (dto: SaveFilterItemDto) => Promise<void>;
}

/** Adds a filter widget: a set of filter clauses whose values are typed on the board,
    re-filtering the Analytic/Entries widgets wired to it. */
export function FilterWidgetForm({ color, onBack, onAdd }: Props) {
    return (
        <FilterForm
            submitLabel="Add"
            color={color}
            onBack={onBack}
            onSubmit={onAdd}
        />
    );
}
