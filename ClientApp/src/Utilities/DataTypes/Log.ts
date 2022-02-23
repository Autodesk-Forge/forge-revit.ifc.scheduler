import {computed} from "mobx";

export class Log{
    public readonly name: string = '';

    constructor({name}: {name: string}) {
        this.name = name;
    }

    @computed public get searchTerm(){return `${this.name}`.toLowerCase()}
}