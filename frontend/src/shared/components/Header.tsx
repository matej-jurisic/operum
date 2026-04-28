import {
    Button,
    Group,
    Menu,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { observer } from "mobx-react";
import { JSX, useState } from "react";
import { CiLogout, CiSettings, CiUser } from "react-icons/ci";
import { GoSun } from "react-icons/go";
import { IoMoonOutline } from "react-icons/io5";
import { useNavigate } from "react-router-dom";
import AuthDialog from "../../features/auth/components/AuthDialog";
import useAuth from "../../features/auth/hooks/useAuth";
import globalStore from "../stores/GlobalStore";
import BackButton from "./BackButton";

interface Props {
    color?: string;
    items?: JSX.Element[];
}

const Header = observer((props: Props) => {
    const { colorScheme, toggleColorScheme } = useMantineColorScheme();
    const theme = useMantineTheme();
    const auth = useAuth();
    const navigate = useNavigate();

    const [isOpenAuth, setIsOpenAuth] = useState(false);

    return (
        <Group align="center" justify="flex-end">
            <Group>
                {globalStore.currentUser && <BackButton color={props.color} />}
                <Button
                    variant="outline"
                    color={props.color ?? theme.primaryColor}
                    onClick={() => toggleColorScheme()}
                >
                    {colorScheme === "light" ? (
                        <IoMoonOutline size={16} />
                    ) : (
                        <GoSun size={16} />
                    )}
                </Button>
                <Menu zIndex={400}>
                    <Menu.Target>
                        <Button
                            variant="outline"
                            color={props.color ?? theme.primaryColor}
                        >
                            <CiUser size={18} />
                        </Button>
                    </Menu.Target>

                    <Menu.Dropdown>
                        {globalStore.currentUser ? (
                            <>
                                <Menu.Item
                                    leftSection={<CiUser size={16} />}
                                    onClick={() => navigate("/profile")}
                                >
                                    {globalStore.currentUser?.userName ||
                                        "Guest"}
                                </Menu.Item>
                            </>
                        ) : (
                            <Menu.Item
                                leftSection={<CiUser size={16} />}
                                onClick={() => setIsOpenAuth(true)}
                            >
                                {"Login"}
                            </Menu.Item>
                        )}
                        {globalStore.userHasRole("admin") && (
                            <Menu.Item
                                leftSection={<CiSettings size={16} />}
                                onClick={() => navigate("/admin-panel")}
                            >
                                Admin panel
                            </Menu.Item>
                        )}
                        {globalStore.currentUser && (
                            <Menu.Item
                                leftSection={<CiLogout size={16} />}
                                onClick={async () => {
                                    await auth.logout();
                                }}
                            >
                                Logout
                            </Menu.Item>
                        )}
                    </Menu.Dropdown>
                </Menu>
            </Group>

            {isOpenAuth && <AuthDialog onClose={() => setIsOpenAuth(false)} />}
        </Group>
    );
});

export default Header;
