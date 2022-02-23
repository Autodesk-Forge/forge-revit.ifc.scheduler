import {computed} from "mobx";
import {Project} from "./Project";
import {Folder} from "./Folder";

export interface ITreeBase {
    id: string;
    name: string;
    type?: string;
    project: Project;
    parent?: Folder
}

export abstract class TreeBase{
    public readonly id: string;
    public readonly name: string;
    public get value(){return this.id};
    public get label(){return this.name};
    public readonly project: Project;
    public readonly parent?: Folder;

    constructor({id, name, project, parent}: ITreeBase) {
        this.id = id;
        this.name = name;
        this.project = project;
        this.parent = parent;
    }

    public abstract getTree(): any;
    public isChecked(checked?: string[]){
        return (checked || this.project.checked).indexOf(this.id) > -1;
    }

    @computed public get searchTerm(){return `${this.name}`.toLowerCase()}
}