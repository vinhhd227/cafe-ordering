import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import { definePreset } from '@primeuix/themes'
// Directives
import Ripple from 'primevue/ripple'
import KeyFilter from 'primevue/keyfilter'
// Components
import Button from 'primevue/button'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import Toolbar from 'primevue/toolbar'
import Avatar from 'primevue/avatar'
import Menu from 'primevue/menu'
import PanelMenu from 'primevue/panelmenu'
import Badge from 'primevue/badge'
import ScrollTop from 'primevue/scrolltop'
import Divider from 'primevue/divider'
import Breadcrumb from 'primevue/breadcrumb'
import TieredMenu from 'primevue/tieredmenu'
import OverlayBadge from 'primevue/overlaybadge'
import Card from 'primevue/card'
import Select from 'primevue/select'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import ColumnGroup from 'primevue/columngroup'
import Row from 'primevue/row'
import Paginator from 'primevue/paginator'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { Form } from '@primevue/forms'
import Panel from 'primevue/panel'
import ToggleSwitch from 'primevue/toggleswitch'
import Message from 'primevue/message'
import SpeedDial from 'primevue/speeddial'
import InputNumber from 'primevue/inputnumber'
import InputGroup from 'primevue/inputgroup'
import InputGroupAddon from 'primevue/inputgroupaddon'
import Carousel from 'primevue/carousel'
import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Textarea from 'primevue/textarea'
import Toast from 'primevue/toast'
import Password from 'primevue/password';
import FloatLabel from 'primevue/floatlabel'
import ToastService from 'primevue/toastservice'
import Checkbox from 'primevue/checkbox';
import CheckboxGroup from 'primevue/checkboxgroup';
import TabView from 'primevue/tabview';
import TabPanel from 'primevue/tabpanel';

// Theme
const coffeePreset = definePreset(Aura, {
    semantic: {
        primary: {
            50: '#f6ede6',
            100: '#ecd8c9',
            200: '#dec2a2',
            300: '#cfa06b',
            400: '#bd7b45',
            500: '#af7341', // PRIMARY
            600: '#9a6238',
            700: '#7f512f',
            800: '#654026',
            900: '#4b301d',
        },
        surface: {
            0: '#ffffff',
            50: '#FAF7F2', // nền chính content
            100: '#F2EDE6', // section xen kẽ
            200: '#E6DED4', // border / divider
        },
        text: {
            primary: '#2B1D14', // text chính
            secondary: '#6B5A4D', // text phụ
            muted: '#8B7A6C',
            inverse: '#FAF7F2', // text trên nền tối
        },
        accent: {
            500: '#E3A857', // vàng mật ong
        },
    },
    //Your customizations, see the following sections for examples
})
export default {
    install(app) {
        app.use(ToastService)
        app.use(PrimeVue, {
            theme: {
                preset: Aura,
                options: {
                    darkModeSelector: '.app-dark',
                    cssLayer: false,
                },
            },
            ripple: true,
        })

        app.directive('ripple', Ripple)
        app.directive('keyfilter', KeyFilter)
        app.component('prime-button', Button)
        app.component('prime-icon-field', IconField)
        app.component('prime-input-icon', InputIcon)
        app.component('prime-input-text', InputText)
        app.component('prime-toolbar', Toolbar)
        app.component('prime-avatar', Avatar)
        app.component('prime-menu', Menu)
        app.component('prime-panel-menu', PanelMenu)
        app.component('prime-badge', Badge)
        app.component('prime-scroll-top', ScrollTop)
        app.component('prime-divider', Divider)
        app.component('prime-breadcrumb', Breadcrumb)
        app.component('prime-tiered-menu', TieredMenu)
        app.component('prime-overlay-badge', OverlayBadge)
        app.component('prime-card', Card)
        app.component('prime-select', Select)
        app.component('prime-data-table', DataTable)
        app.component('prime-column', Column)
        app.component('prime-column-group', ColumnGroup)
        app.component('prime-row', Row)
        app.component('prime-paginator', Paginator)
        app.component('prime-skeleton', Skeleton)
        app.component('prime-tag', Tag)
        app.component('prime-form', Form)
        app.component('prime-panel', Panel)
        app.component('prime-toggle-switch', ToggleSwitch)
        app.component('prime-message', Message)
        app.component('prime-speed-dial', SpeedDial)
        app.component('prime-input-number', InputNumber)
        app.component('prime-input-group', InputGroup)
        app.component('prime-input-group-addon', InputGroupAddon)
        app.component('prime-carousel', Carousel)
        app.component('prime-accordion', Accordion)
        app.component('prime-accordion-panel', AccordionPanel)
        app.component('prime-accordion-header', AccordionHeader)
        app.component('prime-accordion-content', AccordionContent)
        app.component('prime-textarea', Textarea)
        app.component('prime-password', Password)
        app.component('prime-float-label', FloatLabel)
        app.component('prime-toast', Toast)
        app.component('prime-checkbox', Checkbox)
        app.component('prime-checkbox-group', CheckboxGroup)
        app.component('prime-tab-view', TabView)
        app.component('prime-tab-panel', TabPanel)
    },
}
