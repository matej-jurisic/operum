import {
  Button,
  Combobox,
  Group,
  Popover,
  Text,
  useCombobox,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import { CiFilter } from "react-icons/ci";

import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../context/ViewsContext";

export default function SelectViewMenu() {
  const { selectedViewId, tracker } = useTracker();
  const { setSelectedView } = useTrackerOperations();
  const { views, refreshViewsIfDirty } = useViews();

  const isMobile = useMediaQuery("(max-width: 48em)");

  const [opened, setOpened] = useState(false);

  const combobox = useCombobox({
    onDropdownClose: () => combobox.resetSelectedOption(),
  });

  useEffect(() => {
    refreshViewsIfDirty();
  }, []);

  return (
    <Popover
      opened={opened}
      onChange={setOpened}
      position="bottom-end"
      withArrow
      shadow="md"
      width={280}
    >
      <Popover.Target>
        <Button
          variant={selectedViewId ? "filled" : "outline"}
          color={tracker.color}
          px={isMobile ? "xs" : undefined}
          aria-label="Select view"
          style={{ flexShrink: 0 }}
          onClick={() => {
            setOpened((current) => !current);
            combobox.toggleDropdown();
          }}
        >
          <CiFilter size={18} />
        </Button>
      </Popover.Target>

      <Popover.Dropdown p={0}>
        <Combobox
          store={combobox}
          onOptionSubmit={async (value) => {
            await setSelectedView(value || null);

            setOpened(false);
            combobox.closeDropdown();
          }}
        >
          <Combobox.Options>
            <Combobox.Option value="">
              <Group justify="space-between">
                <Text size="sm" c="dimmed" fs="italic">
                  None
                </Text>

                {selectedViewId === null && <Text c={tracker.color}>✓</Text>}
              </Group>
            </Combobox.Option>

            {views.map((view) => (
              <Combobox.Option key={view.id} value={view.id}>
                <Group justify="space-between">
                  <Text size="sm" truncate>
                    {view.name}
                  </Text>

                  {selectedViewId === view.id && (
                    <Text c={tracker.color}>✓</Text>
                  )}
                </Group>
              </Combobox.Option>
            ))}
          </Combobox.Options>
        </Combobox>
      </Popover.Dropdown>
    </Popover>
  );
}
