import PrimeVue from "primevue/config";
import Aura from "@primeuix/themes/aura";
import { definePreset } from "@primeuix/themes";

// Components
/// Form
import { Form } from "@primevue/forms";
import AutoComplete from "primevue/autocomplete";
import CascadeSelect from "primevue/cascadeselect";
import Checkbox from "primevue/checkbox";
import CheckboxGroup from "primevue/checkboxgroup";
import ColorPicker from "primevue/colorpicker";
import DatePicker from "primevue/datepicker";
import Editor from "primevue/editor";
import FloatLabel from "primevue/floatlabel";
import IconField from "primevue/iconfield";
import InputIcon from "primevue/inputicon";
import IftaLabel from "primevue/iftalabel";
import InputGroup from "primevue/inputgroup";
import InputGroupAddon from "primevue/inputgroupaddon";
import InputMask from "primevue/inputmask";
import InputNumber from "primevue/inputnumber";
import InputOtp from "primevue/inputotp";
import InputText from "primevue/inputtext";
import KeyFilter from "primevue/keyfilter";
import Knob from "primevue/knob";
import Listbox from "primevue/listbox";
import MultiSelect from "primevue/multiselect";
import Password from "primevue/password";
import RadioButton from "primevue/radiobutton";
import RadioButtonGroup from "primevue/radiobuttongroup";
import Rating from "primevue/rating";
import Select from "primevue/select";
import SelectButton from "primevue/selectbutton";
import Slider from "primevue/slider";
import Textarea from "primevue/textarea";
import ToggleButton from "primevue/togglebutton";
import ToggleSwitch from "primevue/toggleswitch";
import TreeSelect from "primevue/treeselect";
/// Button
import Button from "primevue/button";
import SpeedDial from "primevue/speeddial";
import SplitButton from "primevue/splitbutton";
/// Data
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import ColumnGroup from "primevue/columngroup"; // optional
import Row from "primevue/row";
import DataView from "primevue/dataview";
import OrderList from "primevue/orderlist";
import OrganizationChart from "primevue/organizationchart";
import Paginator from "primevue/paginator";
import PickList from "primevue/picklist";
import Timeline from "primevue/timeline";
import Tree from "primevue/tree";
import TreeTable from "primevue/treetable";
import VirtualScroller from "primevue/virtualscroller";
/// Panel
import Accordion from "primevue/accordion";
import AccordionPanel from "primevue/accordionpanel";
import AccordionHeader from "primevue/accordionheader";
import AccordionContent from "primevue/accordioncontent";
import Card from "primevue/card";
import DeferredContent from "primevue/deferredcontent";
import Divider from "primevue/divider";
import Fieldset from "primevue/fieldset";
import Panel from "primevue/panel";
import ScrollPanel from "primevue/scrollpanel";
import Splitter from "primevue/splitter";
import SplitterPanel from "primevue/splitterpanel";
import Stepper from "primevue/stepper";
import StepList from "primevue/steplist";
import StepPanels from "primevue/steppanels";
import StepItem from "primevue/stepitem";
import Step from "primevue/step";
import StepPanel from "primevue/steppanel";
import Tabs from "primevue/tabs";
import TabList from "primevue/tablist";
import Tab from "primevue/tab";
import TabPanels from "primevue/tabpanels";
import TabPanel from "primevue/tabpanel";
import Toolbar from "primevue/toolbar";
/// Overlay
import ConfirmDialog from "primevue/confirmdialog";
import ConfirmationService from "primevue/confirmationservice";
import ConfirmPopup from "primevue/confirmpopup";
import Dialog from "primevue/dialog";
import Drawer from "primevue/drawer";
import DynamicDialog from "primevue/dynamicdialog";
import Popover from "primevue/popover";
import Tooltip from "primevue/tooltip";
/// File
import FileUpload from "primevue/fileupload";
///Menu
import Breadcrumb from "primevue/breadcrumb";
import ContextMenu from "primevue/contextmenu";
import Dock from "primevue/dock";
import Menu from "primevue/menu";
import Menubar from "primevue/menubar";
import MegaMenu from "primevue/megamenu";
import PanelMenu from "primevue/panelmenu";
import TieredMenu from "primevue/tieredmenu";
/// Chart
import Chart from "primevue/chart";
/// Message
import Message from "primevue/message";
import Toast from "primevue/toast";
import ToastService from "primevue/toastservice";
/// Media
import Carousel from "primevue/carousel";
import Galleria from "primevue/galleria";
import Image from "primevue/image";
import ImageCompare from "primevue/imagecompare";
/// Misc
import AnimateOnScroll from "primevue/animateonscroll";
import Avatar from "primevue/avatar";
import AvatarGroup from "primevue/avatargroup"; //Optional for grouping
import Badge from "primevue/badge";
import OverlayBadge from "primevue/overlaybadge";
import BlockUI from "primevue/blockui";
import Chip from "primevue/chip";
import FocusTrap from "primevue/focustrap";
import Fluid from "primevue/fluid";
import Inplace from "primevue/inplace";
import MeterGroup from "primevue/metergroup";
import ProgressBar from "primevue/progressbar";
import ProgressSpinner from "primevue/progressspinner";
import ScrollTop from "primevue/scrolltop";
import Skeleton from "primevue/skeleton";
import Ripple from "primevue/ripple";
import StyleClass from "primevue/styleclass";
import Tag from "primevue/tag";
import Terminal from "primevue/terminal";
import TerminalService from "primevue/terminalservice";
/// Custom
import AppAlert from "@/components/AppAlert.vue";
// Theme
const coffeePreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: "{emerald.50}",
      100: "{emerald.100}",
      200: "{emerald.200}",
      300: "{emerald.300}",
      400: "{emerald.400}",
      500: "{emerald.500}",
      600: "{emerald.600}",
      700: "{emerald.700}",
      800: "{emerald.800}",
      900: "{emerald.900}",
      950: "{emerald.950}",
    },
    surface: {
      0: "#ffffff",
      50: "#f9fafb",
      100: "#f3f4f6",
      200: "#e5e7eb",
      300: "#d1d5db",
      400: "#9ca3af",
      500: "#6b7280",
      600: "#4b5563",
      700: "#374151",
      800: "#1f2937",
      900: "#111827",
      950: "#030712",
    },
    text: {
      primary: "#2B1D14", // text chính
      secondary: "#6B5A4D", // text phụ
      muted: "#8B7A6C",
      inverse: "#FFFFFF", // text trên nền tối
    },
  },
  // Component overrides — card & panel dùng màu solid
  components: {
    datatable: {
      background: "{transparent}",
      shadow: "0 1px 3px 0 rgba(0,0,0,.08), 0 1px 2px -1px rgba(0,0,0,.06)",
    },
    card: {
      background: "#ffffff",
    },
    panel: {
      background: "{surface.0}",
    },
  },
});
export default {
  install(app) {
    app.use(ToastService);
    app.use(ConfirmationService);
    app.use(TerminalService);
    app.use(PrimeVue, {
      theme: {
        preset: coffeePreset,
        options: {
          darkModeSelector: ".app-dark",
          cssLayer: false,
        },
      },
      ripple: true,
    });
    /// Form
    app.component("prime-form", Form);
    app.component("prime-auto-complete", AutoComplete);
    app.component("prime-cascade-select", CascadeSelect);
    app.component("prime-checkbox", Checkbox);
    app.component("prime-checkbox-group", CheckboxGroup);
    app.component("prime-color-picker", ColorPicker);
    app.component("prime-date-picker", DatePicker);
    app.component("prime-editor", Editor);
    app.component("prime-float-label", FloatLabel);
    app.component("prime-icon-field", IconField);
    app.component("prime-input-icon", InputIcon);
    app.component("prime-ifta-label", IftaLabel);
    app.component("prime-input-group", InputGroup);
    app.component("prime-input-group-addon", InputGroupAddon);
    app.component("prime-input-mask", InputMask);
    app.component("prime-input-number", InputNumber);
    app.component("prime-input-otp", InputOtp);
    app.component("prime-input-text", InputText);
    app.directive("keyfilter", KeyFilter);
    app.component("prime-knob", Knob);
    app.component("prime-listbox", Listbox);
    app.component("prime-multi-select", MultiSelect);
    app.component("prime-password", Password);
    app.component("prime-radio-button", RadioButton);
    app.component("prime-radio-button-group", RadioButtonGroup);
    app.component("prime-rating", Rating);
    app.component("prime-select", Select);
    app.component("prime-select-button", SelectButton);
    app.component("prime-slider", Slider);
    app.component("prime-textarea", Textarea);
    app.component("prime-toggle-button", ToggleButton);
    app.component("prime-toggle-switch", ToggleSwitch);
    app.component("prime-tree-select", TreeSelect);
    /// Button
    app.component("prime-button", Button);
    app.component("prime-speed-dial", SpeedDial);
    app.component("prime-split-button", SplitButton);
    /// Data
    app.component("prime-data-table", DataTable);
    app.component("prime-column", Column);
    app.component("prime-column-group", ColumnGroup);
    app.component("prime-row", Row);
    app.component("prime-dataview", DataView);
    app.component("prime-order-list", OrderList);
    app.component("prime-organization-chart", OrganizationChart);
    app.component("prime-paginator", Paginator);
    app.component("prime-pick-list", PickList);
    app.component("prime-timeline", Timeline);
    app.component("prime-tree", Tree);
    app.component("prime-tree-table", TreeTable);
    app.component("prime-virtual-scroller", VirtualScroller);
    /// Panel
    app.component("prime-accordion", Accordion);
    app.component("prime-accordion-panel", AccordionPanel);
    app.component("prime-accordion-header", AccordionHeader);
    app.component("prime-accordion-content", AccordionContent);
    app.component("prime-card", Card);
    app.component("prime-deferred-content", DeferredContent);
    app.component("prime-divider", Divider);
    app.component("prime-fieldset", Fieldset);
    app.component("prime-panel", Panel);
    app.component("prime-scroll-panel", ScrollPanel);
    app.component("prime-splitter", Splitter);
    app.component("prime-splitter-panel", SplitterPanel);
    app.component("prime-stepper", Stepper);
    app.component("prime-step-list", StepList);
    app.component("prime-step-panels", StepPanels);
    app.component("prime-step-item", StepItem);
    app.component("prime-step", Step);
    app.component("prime-step-panel", StepPanel);
    app.component("prime-tabs", Tabs);
    app.component("prime-tab-list", TabList);
    app.component("prime-tab", Tab);
    app.component("prime-tab-panels", TabPanels);
    app.component("prime-tab-panel", TabPanel);
    app.component("prime-toolbar", Toolbar);
    /// Overlay
    app.component("prime-confirm-dialog", ConfirmDialog);
    app.component("prime-confirm-popup", ConfirmPopup);
    app.component("prime-dialog", Dialog);
    app.component("prime-drawer", Drawer);
    app.component("prime-dynamic-dialog", DynamicDialog);
    app.component("prime-popover", Popover);
    app.directive("tooltip", Tooltip);
    /// File
    app.component("prime-file-upload", FileUpload);
    ///Menu
    app.component("prime-breadcrumb", Breadcrumb);
    app.component("prime-context-menu", ContextMenu);
    app.component("prime-dock", Dock);
    app.component("prime-menu", Menu);
    app.component("prime-menubar", Menubar);
    app.component("prime-megamenu", MegaMenu);
    app.component("prime-panel-menu", PanelMenu);
    app.component("prime-tiered-menu", TieredMenu);
    /// Chart
    app.component("prime-chart", Chart);
    /// Message
    app.component("prime-message", Message);
    app.component("prime-toast", Toast);
    /// Media
    app.component("prime-carousel", Carousel);
    app.component("prime-galleria", Galleria);
    app.component("prime-image", Image);
    app.component("prime-image-compare", ImageCompare);
    /// Misc
    app.directive("animateonscroll", AnimateOnScroll);
    app.component("prime-avatar", Avatar);
    app.component("prime-avatar-group", AvatarGroup);
    app.component("prime-badge", Badge);
    app.component("prime-overlay-badge", OverlayBadge);
    app.component("prime-block-ui", BlockUI);
    app.component("prime-chip", Chip);
    app.directive("focustrap", FocusTrap);
    app.component("prime-fluid", Fluid);
    app.component("prime-inplace", Inplace);
    app.component("prime-meter-group", MeterGroup);
    app.component("prime-progress-bar", ProgressBar);
    app.component("prime-progress-spinner", ProgressSpinner);
    app.component("prime-scroll-top", ScrollTop);
    app.component("prime-skeleton", Skeleton);
    app.directive("ripple", Ripple);
    app.directive("styleclass", StyleClass);
    app.component("prime-tag", Tag);
    app.component("prime-terminal", Terminal);
    /// Custom
    app.component("app-alert", AppAlert);
  },
};
