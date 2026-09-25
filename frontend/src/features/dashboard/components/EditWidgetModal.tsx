import {
  Button,
  Checkbox,
  Group,
  Modal,
  NumberInput,
  Paper,
  SegmentedControl,
  Stack,
  Text,
  TextInput,
} from "@mantine/core";
import { TimePicker } from "@mantine/dates";
import { useEffect, useMemo, useState } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { ColorSwatchPicker } from "../../../shared/components/ColorSwatchPicker";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import {
  DashboardItemDisplayMode,
  DashboardItemSourceDto,
  GoalConditionalTargetDto,
  parseFilterWidgetConfig,
  UpdateDashboardItemDto,
  WidgetTypes,
} from "../types/DashboardDto";
import { WidgetDisplayModeFields } from "./WidgetDisplayModeFields";
import { SourceViewSelect } from "./SourceViewSelect";
import { YAxisScaleOption } from "./YAxisScaleOption";
import { CalendarStartMonthOption } from "./CalendarStartMonthOption";
import { DATE_TYPES } from "./filterClauseInput";
import { FilterFollowChecklist } from "./FilterFollowChecklist";
import {
  connectedClausesFromLinks,
  filterCandidatesFor,
  followLinksComplete,
} from "./filterLinkUtils";
import { GoalConditionalTargetsEditor } from "./GoalConditionalTargetsEditor";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";
import { GoalDirection, GoalDirections } from "../../analytics/types/AnalyticDto";

interface Props {
  itemId: string;
  color: string;
  onClose: () => void;
  onSave: (itemId: string, dto: UpdateDashboardItemDto) => Promise<void>;
}

/** One source's editable half, alongside the parts of it the form only shows. */
interface SourceRow {
  source: DashboardItemSourceDto;
  label: string;
  viewId: string | null;
  views: ViewDto[];
  fields: FieldDto[];
  // filterItemId -> (that filter's clause slot id -> field of this source's tracker it maps to).
  filterLinks: Record<string, Record<string, string>>;
}

// Goal calculations that always come out as a plain number, whatever the field type. Mirrors
// CustomAnalyticForm's copy of the same list.
const GOAL_COUNTING_CODES = [
  "Count",
  "Count Distinct",
  "True Count",
  "False Count",
  "True Percentage",
];

/** The chart drawn is the definition it was added with; changing it means adding a new widget. */
export function EditWidgetModal({ itemId, color, onClose, onSave }: Props) {
  const { dashboardId, widgets, syncFilterFollows } = useDashboard();
  const filterCandidates = useMemo(() => filterCandidatesFor(widgets), [widgets]);
  const [rows, setRows] = useState<SourceRow[] | null>(null);
  const [name, setName] = useState("");
  const [nameFallback, setNameFallback] = useState("");
  const [code, setCode] = useState("");
  const [displayMode, setDisplayMode] = useState(DashboardItemDisplayMode.Full);
  const [mobileDisplayMode, setMobileDisplayMode] = useState(
    DashboardItemDisplayMode.Full,
  );
  const [isLineChart, setIsLineChart] = useState(false);
  const [isBarChart, setIsBarChart] = useState(false);
  const [isCalendar, setIsCalendar] = useState(false);
  const [isGoal, setIsGoal] = useState(false);
  const [isSingleValue, setIsSingleValue] = useState(false);
  const [yAxisFromZero, setYAxisFromZero] = useState(true);
  const [matchedValuesOnly, setMatchedValuesOnly] = useState(false);
  const [goalTarget, setGoalTarget] = useState("");
  const [goalDirection, setGoalDirection] = useState<GoalDirection>(
    GoalDirections.HigherIsBetter,
  );
  const [conditionalTargets, setConditionalTargets] = useState<
    GoalConditionalTargetDto[]
  >([]);
  const [colorOverride, setColorOverride] = useState<string | null>(null);
  const [showTrend, setShowTrend] = useState(true);
  const [calendarStartMonth, setCalendarStartMonth] = useState<string | null>(
    null,
  );
  const [trendValueFieldType, setTrendValueFieldType] = useState<string | undefined>(
    undefined,
  );
  const [fieldNameById, setFieldNameById] = useState<Record<string, string>>(
    {},
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  // The Value field's type, already resolved for the trend checkbox above -- a Goal's
  // target is a duration input only when that field is one and the calculation isn't a
  // plain count (which always comes out as a number regardless of field type).
  const goalTargetIsDuration =
    trendValueFieldType === "timespan" && !GOAL_COUNTING_CODES.includes(code);
  const goalTargetValid = goalTargetIsDuration
    ? /^\d+:[0-5]\d:[0-5]\d$/.test(goalTarget.trim())
    : goalTarget.trim() !== "" && Number.isFinite(Number(goalTarget.trim()));

  // The only clauses a conditional target may key off: derived live from the row's
  // in-progress filter-follow selection, same as CustomAnalyticForm does at creation --
  // so unchecking a follow here immediately drops any conditional target that relied on it.
  const connectedClauses = useMemo(
    () =>
      connectedClausesFromLinks(
        rows?.[0]?.filterLinks ?? {},
        filterCandidates,
        fieldNameById,
      ),
    [rows, filterCandidates, fieldNameById],
  );

  // The render endpoint carries calculated charts, not definitions, so read from dashboard itself.
  useEffect(() => {
    const load = async () => {
      const res = await dashboardController.getDashboard(dashboardId);
      const item = res.data?.items.find((i) => i.id === itemId);

      if (!item || item.type !== WidgetTypes.Analytic) {
        onClose();
        return;
      }

      const sources = [...item.sources].sort((a, b) => a.order - b.order);
      const viewsByTracker = new Map<string, ViewDto[]>();
      const fieldsByTracker = new Map<string, FieldDto[]>();

      const fieldNames: Record<string, string> = {};
      const fieldTypes: Record<string, string> = {};

      await Promise.all(
        [...new Set(sources.map((s) => s.trackerId))].map(async (trackerId) => {
          const [views, fields] = await Promise.all([
            viewsController.getViewList(trackerId),
            fieldsController.getFields(trackerId),
          ]);
          viewsByTracker.set(trackerId, views.data ?? []);
          fieldsByTracker.set(trackerId, fields.data ?? []);
          for (const f of fields.data ?? []) {
            fieldNames[f.id] = f.name;
            fieldTypes[f.id] = f.type;
          }
        }),
      );

      setFieldNameById(fieldNames);

      // Min/Max are the only SingleValue codes whose Value field may be Date/DateTime, and a
      // trend can't plot a date, so the checkbox is hidden rather than shown but inert.
      const valueFieldId = sources[0]?.fields.find((f) => f.purpose === "Value")?.fieldId;
      setTrendValueFieldType(valueFieldId ? fieldTypes[valueFieldId] : undefined);

      // Reconstructs each source's current "follow filters" selection from the board's filter
      // widgets, whose config already keys fieldByQuery by clause slot id -- exactly what
      // FilterFollowChecklist expects, no translation needed.
      const filterLinksFor = (trackerId: string) => {
        const out: Record<string, Record<string, string>> = {};
        for (const w of widgets) {
          if (w.type !== WidgetTypes.Filter) continue;
          const link = parseFilterWidgetConfig(w.config)?.links.find(
            (l) => l.itemId === itemId && l.trackerId === trackerId,
          );
          if (link) out[w.id] = link.fieldByQuery;
        }
        return out;
      };

      setRows(
        sources.map((source) => ({
          source,
          label: source.label ?? "",
          viewId: source.viewId ?? null,
          views: viewsByTracker.get(source.trackerId) ?? [],
          fields: fieldsByTracker.get(source.trackerId) ?? [],
          filterLinks: filterLinksFor(source.trackerId),
        })),
      );
      setName(item.rawName ?? "");
      setNameFallback(item.name);
      setCode(item.code);
      setDisplayMode(item.layout.displayMode);
      setMobileDisplayMode(item.mobileLayout.displayMode);
      setIsLineChart(item.resultType === AnalyticResultTypeEnum.LineChart);
      setIsBarChart(item.resultType === AnalyticResultTypeEnum.BarChart);
      setIsCalendar(item.resultType === AnalyticResultTypeEnum.Calendar);
      setIsGoal(item.resultType === AnalyticResultTypeEnum.Goal);
      setIsSingleValue(item.resultType === AnalyticResultTypeEnum.SingleValue);
      setYAxisFromZero(item.yAxisFromZero);
      setMatchedValuesOnly(item.matchedValuesOnly);
      setGoalTarget(item.goalTarget ?? "");
      setGoalDirection(
        (item.goalDirection as GoalDirection | undefined) ?? GoalDirections.HigherIsBetter,
      );
      setConditionalTargets(item.goalConditionalTargets ?? []);
      setColorOverride(item.color ?? null);
      setShowTrend(item.showTrend);
      setCalendarStartMonth(item.calendarStartMonth ?? null);
    };

    load();
    // widgets is read once per open to seed each row's current filter-follow links; it isn't
    // meant to re-run this fetch on every board update while the modal is sitting open.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dashboardId, itemId, onClose]);

  const updateRow = (index: number, changes: Partial<SourceRow>) =>
    setRows((current) =>
      current
        ? current.map((row, i) => (i === index ? { ...row, ...changes } : row))
        : current,
    );

  const handleSubmit = async () => {
    if (!rows) return;

    setIsSubmitting(true);
    try {
      // Applied first: a widget's own goal-conditional-targets validate against whichever
      // filters it currently follows, so the follow links must land before that check runs.
      await syncFilterFollows(
        itemId,
        rows.map((row) => ({ trackerId: row.source.trackerId, links: row.filterLinks })),
      );
      // Sends every source every time: a cleared name/view must arrive as cleared, not missing.
      await onSave(itemId, {
        name: name.trim(),
        displayMode,
        mobileDisplayMode,
        yAxisFromZero,
        matchedValuesOnly,
        goalTarget: isGoal ? goalTarget.trim() : undefined,
        goalDirection: isGoal ? goalDirection : undefined,
        goalConditionalTargets: isGoal ? conditionalTargets : [],
        color: isCombined ? null : colorOverride,
        showTrend,
        calendarStartMonth: isCalendar ? calendarStartMonth : null,
        sources: rows.map((row) => ({
          sourceId: row.source.id,
          label: row.label.trim() || null,
          viewId: row.viewId,
        })),
      });
    } finally {
      setIsSubmitting(false);
    }

    onClose();
  };

  const isCombined = (rows?.length ?? 0) > 1;
  const canShowTrend =
    (isGoal || isSingleValue) &&
    (!trendValueFieldType || !DATE_TYPES.includes(trendValueFieldType));
  const canShowMatchedValuesOnly = isCombined && (isLineChart || isBarChart);
  const linksComplete =
    !!rows && rows.every((row) => followLinksComplete(row.filterLinks, filterCandidates, row.fields));
  const canSubmit = linksComplete && (!isGoal || goalTargetValid);

  return (
    <Modal
      opened
      onClose={onClose}
      title="Edit widget"
      size="lg"
      centered
      overlayProps={{ backgroundOpacity: 0.35 }}
    >
      {/* Global request loader already covers the fetch above. */}
      {rows && (
        <Stack gap="md">
          <TextInput
            label="Name"
            placeholder={nameFallback}
            maxLength={100}
            value={name}
            onChange={(event) => setName(event.currentTarget.value)}
          />

          {rows.map((row, index) => {
            const nameInput = (
              <TextInput
                label={
                  isCombined
                    ? isCalendar
                      ? "Source name"
                      : "Series name"
                    : "Name"
                }
                description={
                  isCombined
                    ? isCalendar
                      ? "Names this tracker in the calendar's legend"
                      : "Names this series in the chart's legend"
                    : undefined
                }
                placeholder={row.source.name}
                maxLength={100}
                value={row.label}
                onChange={(event) =>
                  updateRow(index, {
                    label: event.currentTarget.value,
                  })
                }
              />
            );

            const viewSelect = (
              <SourceViewSelect
                views={row.views}
                value={{ viewId: row.viewId }}
                onChange={(selection) => updateRow(index, selection)}
              />
            );

            return (
              <Paper key={row.source.id} withBorder p="sm" radius="md">
                <Stack gap="sm">
                  {isCombined && (
                    <Stack gap={0}>
                      <Text size="sm" fw={600}>
                        {row.source.trackerName}
                      </Text>
                      <Text size="xs" c="dimmed">
                        {row.source.name}
                      </Text>
                    </Stack>
                  )}
                  {nameInput}
                  {viewSelect}
                  <FilterFollowChecklist
                    fields={row.fields}
                    filters={filterCandidates}
                    links={row.filterLinks}
                    onLinksChange={(filterLinks) => updateRow(index, { filterLinks })}
                  />
                </Stack>
              </Paper>
            );
          })}

          {isGoal &&
            (goalTargetIsDuration ? (
              <TimePicker
                label="Target (hh:mm:ss)"
                withSeconds
                format="24h"
                value={goalTarget}
                onChange={setGoalTarget}
              />
            ) : (
              <NumberInput
                label="Target"
                placeholder="Goal value"
                value={goalTarget === "" ? "" : Number(goalTarget)}
                onChange={(value) => setGoalTarget(value === "" ? "" : String(value))}
              />
            ))}

          {isGoal && (
            <SegmentedControl
              value={goalDirection}
              onChange={(value) => setGoalDirection(value as GoalDirection)}
              data={[
                { label: "Higher is better", value: GoalDirections.HigherIsBetter },
                { label: "Lower is better", value: GoalDirections.LowerIsBetter },
              ]}
            />
          )}

          {isGoal && connectedClauses.length > 0 && (
            <GoalConditionalTargetsEditor
              clauses={connectedClauses}
              value={conditionalTargets}
              onChange={setConditionalTargets}
            />
          )}

          {canShowTrend && (
            <Checkbox
              label="Show trend"
              checked={showTrend}
              onChange={(event) => setShowTrend(event.currentTarget.checked)}
            />
          )}

          {canShowMatchedValuesOnly && (
            <Checkbox
              label="Show only matched values"
              description="Plot only the x-axis values every tracker has data for, so the series cover the same range."
              checked={matchedValuesOnly}
              onChange={(event) => setMatchedValuesOnly(event.currentTarget.checked)}
            />
          )}

          {isCalendar && (
            <CalendarStartMonthOption
              value={calendarStartMonth}
              onChange={setCalendarStartMonth}
            />
          )}

          {isLineChart && (
            <YAxisScaleOption
              yAxisFromZero={yAxisFromZero}
              onChange={setYAxisFromZero}
            />
          )}

          {!isCombined && (
            <ColorSwatchPicker
              label="Color"
              value={colorOverride}
              onChange={setColorOverride}
              allowClear
            />
          )}

          <WidgetDisplayModeFields
            displayMode={displayMode}
            mobileDisplayMode={mobileDisplayMode}
            onDisplayModeChange={setDisplayMode}
            onMobileDisplayModeChange={setMobileDisplayMode}
          />

          <Group justify="flex-end" mt="sm">
            <Button variant="default" onClick={onClose}>
              Cancel
            </Button>
            <Button
              color={color}
              disabled={!canSubmit}
              loading={isSubmitting}
              onClick={handleSubmit}
            >
              Save
            </Button>
          </Group>
        </Stack>
      )}
    </Modal>
  );
}
