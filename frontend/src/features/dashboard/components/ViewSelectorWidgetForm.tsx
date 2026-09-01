import { SaveViewSelectorItemDto } from "../types/DashboardDto";
import { ViewSelectorForm } from "./ViewSelectorForm";

interface Props {
    color?: string;
    onBack: () => void;
    onAdd: (dto: SaveViewSelectorItemDto) => Promise<void>;
}

/** Adds a view selector widget: a dropdown of the board's filter sets that re-filters the
    Analytic widgets wired to it. */
export function ViewSelectorWidgetForm({ color, onBack, onAdd }: Props) {
    return (
        <ViewSelectorForm
            submitLabel="Add"
            color={color}
            onBack={onBack}
            onSubmit={onAdd}
        />
    );
}
