export const inputCustom = 'app-input tw:mt-3! tw:rounded-xl! tw:border! tw:transition! tw:focus:border-primary-400! tw:focus:outline-none! tw:focus:ring-2! tw:focus:ring-primary-300/40!'
export const labelCustom = 'app-label tw:text-xs tw:uppercase tw:tracking-[0.3em]'

const maskIconCustom = 'app-text-subtle tw:hover:text-primary-400! tw:top-[65%]!'
export const passwordCustom = {
    pcinputtext: {
        root: {
            class: inputCustom
        },
    },
    unmaskIcon: {
        class: maskIconCustom
    },
    maskIcon: {
        class: maskIconCustom
    },
    clearicon:{
        class: maskIconCustom
    }

}
