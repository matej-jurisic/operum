import {
    Button,
    Group,
    Menu,
    useComputedColorScheme,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { JSX } from "react";
import { CiLogout, CiUser } from "react-icons/ci";
import { GoSun } from "react-icons/go";
import { IoMoonOutline } from "react-icons/io5";
import useAuth from "../hooks/useAuth";
import globalStore from "../stores/GlobalStore";

interface Props {
    color?: string;
    items?: JSX.Element[];
}

export default function Header(props: Props) {
    const { colorScheme, setColorScheme } = useMantineColorScheme();
    const theme = useMantineTheme();
    const computedColorScheme = useComputedColorScheme("light");
    const auth = useAuth();

    return (
        <Group align="flex-end">
            {props.items}
            <Button
                variant="outline"
                color={props.color ?? theme.primaryColor}
                onClick={() => {
                    setColorScheme(
                        computedColorScheme === "dark" ? "light" : "dark"
                    );
                }}
            >
                {colorScheme === "light" ? (
                    <IoMoonOutline size={16} />
                ) : (
                    <GoSun size={16} />
                )}
            </Button>
            <Menu>
                <Menu.Target>
                    <Button
                        variant="outline"
                        color={props.color ?? theme.primaryColor}
                    >
                        <CiUser size={18} />
                    </Button>
                </Menu.Target>

                <Menu.Dropdown>
                    <Menu.Item leftSection={<CiUser size={16} />}>
                        {globalStore.currentUser?.userName || "Guest"}
                    </Menu.Item>
                    {/* <Menu.Item
                            leftSection={<CiSettings size={16} />}
                            onClick={() => navigate("/admin-panel")}
                        >
                            Admin panel
                        </Menu.Item> */}
                    <Menu.Item
                        leftSection={<CiLogout size={16} />}
                        onClick={async () => {
                            await auth.logout();
                            window.location.reload();
                        }}
                    >
                        Logout
                    </Menu.Item>
                </Menu.Dropdown>
            </Menu>
        </Group>
    );
}
