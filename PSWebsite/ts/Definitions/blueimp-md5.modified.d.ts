// Fixed by David de Regt
declare module 'blueimp-md5' {
    interface md5 {
        (value: string, key?: string, raw?: boolean): string;
    }

    var m: md5;
    export = m;
}
