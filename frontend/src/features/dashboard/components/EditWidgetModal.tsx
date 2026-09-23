import {
  Button,
  Checkbox,
  Group,
  Modal,
  Paper,
  Stack,
  Text,
  TextInput,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useMemo, useState } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
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
import { ConnectedClause } from "./filterLinkUtils";
import { GoalConditionalTargetsEditor } from "./GoalConditionalTargetsEditor";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";

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
}

/** The chart drawn is the definition it was added with; changing it means adding a new widget. */
export function EditWidgetModal({ itemId, color, onClose, onSave }: Props) {
  const { dashboardId, widgets } = useDashboard();
  const isMobile = useMediaQuery("(max-width: 48em)");
  const [rows, setRows] = useState<SourceRow[] | null>(null);
  const [displayMode, setDisplayMode] = useState(DashboardItemDisplayMode.Full);
  const [mobileDisplayMode, setMobileDisplayMode] = useState(
    DashboardItemDisplayMode.Full,
  );
  const [isLineChart, setIsLineChart] = useState(false);
  const [isCalendar, setIsCalendar] = useState(false);
  const [isGoal, setIsGoal] = useState(false);
  const [isSingleValue, setIsSingleValue] = useState(false);
  const [yAxisFromZero, setYAxisFromZero] = useState(true);
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

  // The only clauses a conditional target may key off: read from filter widgets whose
  // config links name this item and the field each clause runs against here.
  const connectedClauses = useMemo<ConnectedClause[]>(() => {
    const out: ConnectedClause[] = [];
    const seen = new Set<string>();
    for (const w of widgets) {
      if (w.type !== WidgetTypes.Filter || !w.filter) continue;
      const config = parseFilterWidgetConfig(w.config);
      if (!config) continue;
      const fieldBySlot: Record<string, string> = {};
      for (const l of config.links)
        if (l.itemId === itemId) Object.assign(fieldBySlot, l.fieldByQuery);
      for (const clause of w.filter.clauses) {
        if (fieldBySlot[clause.slotId] && !seen.has(clause.slotId)) {
          seen.add(clause.slotId);
          out.push({
            slotId: clause.slotId,
            dataType: clause.dataType,
            operator: clause.operator,
            fieldName: fieldNameById[fieldBySlot[clause.slotId]],
          });
        }
      }
    }
    return out;
  }, [widgets, itemId, fieldNameById]);

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

      const fieldNames: Record<string, string> = {};
      const fieldTypes: Record<string, string> = {};

      await Promise.all(
        [...new Set(sources.map((s) => s.trackerId))].map(async (trackerId) => {
          const [views, fields] = await Promise.all([
            viewsController.getViewList(trackerId),
            fieldsController.getFields(trackerId),
          ]);
          viewsByTracker.set(trackerId, views.data ?? []);
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

      setRows(
        sources.map((source) => ({
          source,
          label: source.label ?? "",
          viewId: source.viewId ?? null,
          views: viewsByTracker.get(source.trackerId) ?? [],
        })),
      );
      setDisplayMode(item.layout.displayMode);
      setMobileDisplayMode(item.mobileLayout.displayMode);
      setIsLineChart(item.resultType === AnalyticResultTypeEnum.LineChart);
      setIsCalendar(item.resultType === AnalyticResultTypeEnum.Calendar);
      setIsGoal(item.resultType === AnalyticResultTypeEnum.Goal);
      setIsSingleValue(item.resultType === AnalyticResultTypeEnum.SingleValue);
      setYAxisFromZero(item.yAxisFromZero);
      setConditionalTargets(item.goalConditionalTargets ?? []);
      setColorOverride(item.color ?? null);
      setShowTrend(item.showTrend);
      setCalendarStartMonth(item.calendarStartMonth ?? null);
    };

    load();
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
      // Sends every source every time: a cleared name/view must arrive as cleared, not missing.
      await onSave(itemId, {
        displayMode,
        mobileDisplayMode,
        yAxisFromZero,
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

  return (
    <Modal
      opened
      onClose={onClose}
      title="Edit widget"
      size="lg"
      centered
      fullScreen={isMobile}
      overlayProps={{ backgroundOpacity: 0.35 }}
    >
      {/* Global request loader already covers the fetch above. */}
      {rows && (
        <Stack gap="md">
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
                </Stack>
              </Paper>
            );
          })}

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
            <Button color={color} loading={isSubmitting} onClick={handleSubmit}>
              Save
            </Button>
          </Group>
        </Stack>
      )}
    </Modal>
  );
}
