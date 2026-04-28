import { IconType } from "react-icons";
import {
    TbBarbell,
    TbBook,
    TbBriefcase,
    TbBulb,
    TbCalendar,
    TbCamera,
    TbCar,
    TbChartBar,
    TbCheckbox,
    TbClock,
    TbCode,
    TbCoin,
    TbDatabase,
    TbDeviceGamepad,
    TbEdit,
    TbFilter,
    TbFlask,
    TbHeart,
    TbHome,
    TbLayoutGrid,
    TbList,
    TbMail,
    TbMicroscope,
    TbMoon,
    TbMovie,
    TbMusic,
    TbPalette,
    TbPill,
    TbPlane,
    TbReceipt,
    TbRun,
    TbSchool,
    TbShoppingCart,
    TbStar,
    TbSun,
    TbTarget,
    TbTrophy,
    TbUsers,
    TbVariable,
    TbWallet,
} from "react-icons/tb";

export interface TrackerIconDef {
    name: string;
    label: string;
    icon: IconType;
}

export const CURATED_ICONS: TrackerIconDef[] = [
    { name: "TbLayoutGrid",   label: "Grid",        icon: TbLayoutGrid },
    { name: "TbList",         label: "List",         icon: TbList },
    { name: "TbStar",         label: "Star",         icon: TbStar },
    { name: "TbHeart",        label: "Heart",        icon: TbHeart },
    { name: "TbTarget",       label: "Goals",        icon: TbTarget },
    { name: "TbTrophy",       label: "Achievements", icon: TbTrophy },
    { name: "TbBulb",         label: "Ideas",        icon: TbBulb },
    { name: "TbEdit",         label: "Notes",        icon: TbEdit },
    { name: "TbUsers",        label: "Team",         icon: TbUsers },
    { name: "TbHome",         label: "Home",         icon: TbHome },
    { name: "TbBriefcase",    label: "Work",         icon: TbBriefcase },
    { name: "TbCalendar",     label: "Calendar",     icon: TbCalendar },
    { name: "TbClock",        label: "Time",         icon: TbClock },
    { name: "TbCheckbox",     label: "Tasks",        icon: TbCheckbox },
    { name: "TbFilter",       label: "Tracking",     icon: TbFilter },
    { name: "TbCode",         label: "Code",         icon: TbCode },
    { name: "TbMail",         label: "Mail",         icon: TbMail },
    { name: "TbDatabase",     label: "Database",     icon: TbDatabase },
    { name: "TbVariable",     label: "Formulas",     icon: TbVariable },
    { name: "TbWallet",       label: "Budget",       icon: TbWallet },
    { name: "TbCoin",         label: "Money",        icon: TbCoin },
    { name: "TbShoppingCart", label: "Shopping",     icon: TbShoppingCart },
    { name: "TbReceipt",      label: "Expenses",     icon: TbReceipt },
    { name: "TbChartBar",     label: "Analytics",    icon: TbChartBar },
    { name: "TbBook",         label: "Reading",      icon: TbBook },
    { name: "TbSchool",       label: "Education",    icon: TbSchool },
    { name: "TbBarbell",      label: "Fitness",      icon: TbBarbell },
    { name: "TbRun",          label: "Running",      icon: TbRun },
    { name: "TbPill",         label: "Medication",   icon: TbPill },
    { name: "TbMoon",         label: "Sleep",        icon: TbMoon },
    { name: "TbSun",          label: "Energy",       icon: TbSun },
    { name: "TbMovie",        label: "Movies",       icon: TbMovie },
    { name: "TbMusic",        label: "Music",        icon: TbMusic },
    { name: "TbDeviceGamepad",label: "Gaming",       icon: TbDeviceGamepad },
    { name: "TbCamera",       label: "Photos",       icon: TbCamera },
    { name: "TbPalette",      label: "Art",          icon: TbPalette },
    { name: "TbPlane",        label: "Travel",       icon: TbPlane },
    { name: "TbCar",          label: "Car",          icon: TbCar },
    { name: "TbFlask",        label: "Science",      icon: TbFlask },
    { name: "TbMicroscope",   label: "Research",     icon: TbMicroscope },
];

const ICON_MAP: Record<string, IconType> = Object.fromEntries(
    CURATED_ICONS.map((d) => [d.name, d.icon])
);

export function resolveTrackerIcon(icon?: string): IconType {
    return (icon && ICON_MAP[icon]) || TbLayoutGrid;
}
