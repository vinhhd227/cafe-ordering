import AppAlert from "@/components/AppAlert.vue";
import PrimeVue from "primevue/config";
import Aura from "@primeuix/themes/aura";
import { definePreset } from "@primeuix/themes";
// Directives
import Ripple from "primevue/ripple";
import KeyFilter from "primevue/keyfilter";
// Components
import Button from "primevue/button";
import IconField from "primevue/iconfield";
import InputIcon from "primevue/inputicon";
import InputText from "primevue/inputtext";
import Toolbar from "primevue/toolbar";
import Avatar from "primevue/avatar";
import Menu from "primevue/menu";
import PanelMenu from "primevue/panelmenu";
import Badge from "primevue/badge";
import ScrollTop from "primevue/scrolltop";
import Divider from "primevue/divider";
import Breadcrumb from "primevue/breadcrumb";
import TieredMenu from "primevue/tieredmenu";
import OverlayBadge from "primevue/overlaybadge";
import Card from "primevue/card";
import Select from "primevue/select";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import ColumnGroup from "primevue/columngroup";
import Row from "primevue/row";
import Paginator from "primevue/paginator";
import Skeleton from "primevue/skeleton";
import Tag from "primevue/tag";
import { Form } from "@primevue/forms";
import Panel from "primevue/panel";
import ToggleSwitch from "primevue/toggleswitch";
import Message from "primevue/message";
import SpeedDial from "primevue/speeddial";
import InputNumber from "primevue/inputnumber";
import InputGroup from "primevue/inputgroup";
import InputGroupAddon from "primevue/inputgroupaddon";
import Carousel from "primevue/carousel";
import Accordion from "primevue/accordion";
import AccordionPanel from "primevue/accordionpanel";
import AccordionHeader from "primevue/accordionheader";
import AccordionContent from "primevue/accordioncontent";
import Textarea from "primevue/textarea";
import Toast from "primevue/toast";
import Password from "primevue/password";
import FloatLabel from "primevue/floatlabel";
import ToastService from "primevue/toastservice";
import Checkbox from "primevue/checkbox";
import CheckboxGroup from "primevue/checkboxgroup";
import TabView from "primevue/tabview";
import TabPanel from "primevue/tabpanel";
import Dialog from "primevue/dialog";
import Tooltip from "primevue/tooltip";
import ConfirmPopup from "primevue/confirmpopup";
import Popover from "primevue/popover";
import SelectButton from "primevue/selectbutton";
import DatePicker from "primevue/datepicker";
import ProgressSpinner from "primevue/progressspinner";
import ConfirmDialog from "primevue/confirmdialog";
import MultiSelect from 'primevue/multiselect';
import TreeSelect from "primevue/treeselect";

import ConfirmationService from "primevue/confirmationservice";
import Chart from "primevue/chart";

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
    //   shadow: "0 1px 3px 0 rgba(0,0,0,.08), 0 1px 2px -1px rgba(0,0,0,.06)",
    //   body: {
    //     padding: "0", 
    //   },
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

    app.directive("ripple", Ripple);
    app.directive("keyfilter", KeyFilter);
    app.component("prime-button", Button);
    app.component("prime-icon-field", IconField);
    app.component("prime-input-icon", InputIcon);
    app.component("prime-input-text", InputText);
    app.component("prime-toolbar", Toolbar);
    app.component("prime-avatar", Avatar);
    app.component("prime-menu", Menu);
    app.component("prime-panel-menu", PanelMenu);
    app.component("prime-badge", Badge);
    app.component("prime-scroll-top", ScrollTop);
    app.component("prime-divider", Divider);
    app.component("prime-breadcrumb", Breadcrumb);
    app.component("prime-tiered-menu", TieredMenu);
    app.component("prime-overlay-badge", OverlayBadge);
    app.component("prime-card", Card);
    app.component("prime-select", Select);
    app.component("prime-data-table", DataTable);
    app.component("prime-column", Column);
    app.component("prime-column-group", ColumnGroup);
    app.component("prime-row", Row);
    app.component("prime-paginator", Paginator);
    app.component("prime-skeleton", Skeleton);
    app.component("prime-tag", Tag);
    app.component("prime-form", Form);
    app.component("prime-panel", Panel);
    app.component("prime-toggle-switch", ToggleSwitch);
    app.component("prime-message", Message);
    app.component("prime-speed-dial", SpeedDial);
    app.component("prime-input-number", InputNumber);
    app.component("prime-input-group", InputGroup);
    app.component("prime-input-group-addon", InputGroupAddon);
    app.component("prime-carousel", Carousel);
    app.component("prime-accordion", Accordion);
    app.component("prime-accordion-panel", AccordionPanel);
    app.component("prime-accordion-header", AccordionHeader);
    app.component("prime-accordion-content", AccordionContent);
    app.component("prime-textarea", Textarea);
    app.component("prime-password", Password);
    app.component("prime-float-label", FloatLabel);
    app.component("prime-toast", Toast);
    app.component("prime-checkbox", Checkbox);
    app.component("prime-checkbox-group", CheckboxGroup);
    app.component("prime-tab-view", TabView);
    app.component("prime-tab-panel", TabPanel);
    app.component("prime-dialog", Dialog);
    app.directive("tooltip", Tooltip);
    app.component("prime-confirm-popup", ConfirmPopup);
    app.component("prime-popover", Popover);
    app.component("prime-alert", AppAlert);
    app.component("prime-select-button", SelectButton);
    app.component("prime-date-picker", DatePicker);
    app.component("prime-progress-spinner", ProgressSpinner);
    app.component("prime-confirm-dialog", ConfirmDialog);
    app.component("prime-chart", Chart);
    app.component("prime-multi-select", MultiSelect);
    app.component("prime-tree-select", TreeSelect);
  },
};
