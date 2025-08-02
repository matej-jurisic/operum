import { Flex } from "@mantine/core";
import TrackerList from "../components/TrackerList";

export default function Home() {
    return (
        <>
            <Flex
                direction="column"
                align={"flex-end"}
                style={{ padding: "20px" }}
            >
                <TrackerList />
            </Flex>
        </>
    );
}
